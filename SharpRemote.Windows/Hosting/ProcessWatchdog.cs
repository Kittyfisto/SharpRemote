using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using SharpRemote.Hosting.OutOfProcess;
using log4net;

namespace SharpRemote.Hosting
{
	/// <summary>
	///     Responsible for starting and monitoring another process.
	/// </summary>
	public sealed class ProcessWatchdog
		: IDisposable
	{
		internal const string SharpRemoteHost = "SharpRemote.Host.exe";

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly int _parentPid;

		private readonly PostMortemSettings _postMortemSettings;
		private readonly ProcessStartInfo _startInfo;
		private readonly object _syncRoot;
		private readonly ManualResetEvent _waitHandle;
		private readonly TimeSpan _processReadyTimeout;

		private bool _hasProcessExited;

		private bool _hasProcessFailed;
		private int? _hostedProcessId;
		private HostState _hostedProcessState;
		private bool _isDisposed;
		private bool _isDisposing;
		private Process _process;
		private ProcessFaultReason? _reason;
		private int? _remotePort;
		private Exception _startupException;

		/// <summary>
		///     Initializes a new instance of this ProcessWatchdog with the specified options.
		///     The given host process will only be started once <see cref="Start" /> is called.
		/// </summary>
		/// <param name="process"></param>
		/// <param name="options"></param>
		/// <param name="postMortemSettings">The settings for the post mortem debugger of the host process, if none are specified then no post mortem debugging is performed</param>
		/// <param name="processReadyTimeout">The amount of time the host process has to report being ready before it is assumed to be dead</param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="process" /> is null
		/// </exception>
		/// <exception cref="ArgumentException">
		///     When <paramref name="process" /> is contains only whitespace
		/// </exception>
		public ProcessWatchdog(
			string process = SharpRemoteHost,
			ProcessOptions options = ProcessOptions.HideConsole,
			PostMortemSettings postMortemSettings = null,
			TimeSpan? processReadyTimeout = null
			)
		{
			if (process == null) throw new ArgumentNullException("process");
			if (string.IsNullOrWhiteSpace(process)) throw new ArgumentException("process");
			if (postMortemSettings != null && !postMortemSettings.IsValid)
				throw new ArgumentException("postMortemSettings");

			if (postMortemSettings != null)
			{
				_postMortemSettings = postMortemSettings.Clone();
				if (_postMortemSettings.MinidumpFolder != null)
				{
					_postMortemSettings.MinidumpFolder = _postMortemSettings.MinidumpFolder.Replace('/', '\\');
					if (!_postMortemSettings.MinidumpFolder.EndsWith("\\"))
						_postMortemSettings.MinidumpFolder += '\\';
				}
			}

			_processReadyTimeout = processReadyTimeout ?? new FailureSettings().ProcessReadyTimeout;
			_waitHandle = new ManualResetEvent(false);
			_hostedProcessState = HostState.BootPending;
			_syncRoot = new object();

			_parentPid = Process.GetCurrentProcess().Id;
			_startInfo = new ProcessStartInfo(process)
				{
					Arguments = FormatArguments(_parentPid, _postMortemSettings),
					RedirectStandardOutput = true,
					UseShellExecute = false
				};
			switch (options)
			{
				case ProcessOptions.HideConsole:
					_startInfo.CreateNoWindow = true;
					break;

				case ProcessOptions.ShowConsole:
					_startInfo.CreateNoWindow = false;
					break;
			}


			_hasProcessExited = true;
		}

		/// <summary>
		///     Starts the child process.
		/// </summary>
		/// <exception cref="FileNotFoundException">When the specified executable could not be found</exception>
		/// <exception cref="Win32Exception">When the </exception>
		/// <exception cref="HandshakeException">
		///     The handshake between this and the <see cref="OutOfProcessSiloServer" /> of the remote process failed
		/// </exception>
		public void Start()
		{
			int unused;
			Start(out unused);
		}

		/// <summary>
		///     Starts the child process.
		/// </summary>
		/// <exception cref="FileNotFoundException">When the specified executable could not be found</exception>
		/// <exception cref="Win32Exception">When the </exception>
		/// <exception cref="HandshakeException">
		///     The handshake between this and the <see cref="OutOfProcessSiloServer" /> of the remote process failed
		/// </exception>
		public void Start(out int pid)
		{
			lock (_syncRoot)
			{
				// Make sure to remove everything from the old process
				// and especially make sure that we don't receive events from
				// it!
				if (_process != null)
				{
					_process.Exited -= ProcessOnExited;
					_process.OutputDataReceived -= ProcessOnOutputDataReceived;
				}

				// Prepare the new process
				_process = new Process
				{
					StartInfo = _startInfo,
					EnableRaisingEvents = true,
				};
				_process.Exited += ProcessOnExited;
				_process.OutputDataReceived += ProcessOnOutputDataReceived;
				_startupException = null;
				_remotePort = null;
				_waitHandle.Reset();
			}

			Log.DebugFormat("Starting host '{0}' for parent process (PID: {1})",
							_startInfo.FileName,
							_parentPid);

			StartHostProcess(out pid);
			try
			{
				_hasProcessExited = false;
				_process.BeginOutputReadLine();

				if (!_waitHandle.WaitOne(_processReadyTimeout))
				{
					throw new HandshakeException(string.Format("Process {0} failed to communicate used port number in time ({1}s)",
															   _startInfo.FileName,
															   _processReadyTimeout));
				}

				if (_startupException != null)
				{
					throw new HandshakeException(
						string.Format("Process '{0}' caught an unexpected exception during startup and subsequently failed",
									  _startInfo.FileName),
						_startupException);
				}

				int? port = _remotePort;
				if (port == null)
					throw new HandshakeException(
						string.Format("Process {0} sent the ready signal, but failed to communicate the used port number",
									  _process.StartInfo.FileName));
			}
			catch (Exception e)
			{
				Log.WarnFormat("Caught unexpected exception after having started the host application '{0}' (PID: {1}): {2}",
				               _startInfo.FileName,
				               _hostedProcessId,
				               e);

				_process.TryKill();
				_process.TryDispose();
				_process = null;

				throw;
			}

			Log.InfoFormat("Host '{0}' (PID: {1}) successfully started",
						   _process.StartInfo.FileName,
						   _process.Id);
		}

		/// <summary>
		/// Tries to kill the hosted process.
		/// </summary>
		public void TryKill()
		{
			var id = _hostedProcessId;
			if (id != null)
			{
				lock (_syncRoot)
				{
					if (_process != null)
					{
						_process.Exited -= ProcessOnExited;
						_process.OutputDataReceived -= ProcessOnOutputDataReceived;
					}

					_hostedProcessState = HostState.Dead;
					_remotePort = null;
					_hasProcessFailed = true;
					_hasProcessExited = true;
				}

				ProcessExtensions.TryKill(id.Value);
			}
			_process = null;
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				if (_isDisposed)
					return;

				if (_isDisposing)
					return;

				_isDisposing = true;
			}

			_process.TryKill();
			_process.TryDispose();

			lock (_syncRoot)
			{
				_hasProcessExited = true;
				_hostedProcessId = null;
				_remotePort = null;
				_isDisposed = true;
				_isDisposing = false;
			}
		}

		/// <summary>
		/// The port used by the hosted process.
		/// </summary>
		public int? RemotePort
		{
			get { return _remotePort; }
		}

		/// <summary>
		/// </summary>
		public HostState HostedProcessState
		{
			get { return _hostedProcessState; }
		}

		/// <summary>
		/// </summary>
		[Pure]
		public bool IsProcessRunning
		{
			get { return !_hasProcessExited; }
		}

		/// <summary>
		///     Whether or not the process has failed.
		/// </summary>
		/// <remarks>
		///     False means that the process is either running or has exited on purpose.
		/// </remarks>
		public bool HasProcessFailed
		{
			get { return _hasProcessFailed; }
		}

		/// <summary>
		/// Whether or not this watchdog has been disposed of.
		/// </summary>
		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		/// <summary>
		///     The process-id of the host process, or null, if it's not running.
		/// </summary>
		public int? HostedProcessId
		{
			get { return _hostedProcessId; }
		}

		/// <summary>
		/// The filename of the executable, as given in the constructor.
		/// </summary>
		public string HostExecutableName
		{
			get { return _startInfo.FileName; }
		}

		/// <summary>
		///     This event is invoked whenever the host has written a complete line to its console.
		/// </summary>
		public event Action<string> OnHostOutputWritten;

		/// <summary>
		///     Is invoked when a failure in the remote process has been detected and is invoked prior to handling
		///     this failure.
		/// </summary>
		public event Action<ProcessFaultReason> OnFaultDetected;

		[Pure]
		internal static string FormatArguments(int parentPid, PostMortemSettings postMortemSettings)
		{
			var builder = new StringBuilder();
			builder.Append(parentPid);
			if (postMortemSettings != null)
			{
				builder.Append(" ");
				builder.Append(postMortemSettings.CollectMinidumps);
				builder.Append(" ");
				builder.Append(postMortemSettings.SuppressErrorWindows);
				builder.Append(" ");
				builder.Append(postMortemSettings.HandleAccessViolations);
				builder.Append(" ");
				builder.Append(postMortemSettings.HandleCrtAsserts);
				builder.Append(" ");
				builder.Append(postMortemSettings.HandleCrtPureVirtualFunctionCalls);
				builder.Append(" ");
				builder.Append(((int) postMortemSettings.RuntimeVersions).ToString(CultureInfo.InvariantCulture));
				builder.Append(" ");
				builder.Append(postMortemSettings.NumMinidumpsRetained);
				builder.Append(" ");
				builder.Append(postMortemSettings.MinidumpFolder ?? Path.GetTempPath());
				builder.Append(" ");
				builder.Append(postMortemSettings.MinidumpName ?? "<Unused>");
			}
			return builder.ToString();
		}

		private void ProcessOnExited(object sender, EventArgs args)
		{
			ProcessFaultReason reason;
			lock (_syncRoot)
			{
				// We have to make sure that we ignore events from previously spawned processes!
				if (sender != _process)
					return;

				if (_isDisposed || _isDisposing)
					return;

				if (_reason != null)
					return;

				_reason = reason = ProcessFaultReason.HostProcessExited;
			}

			if (reason != ProcessFaultReason.HostProcessExited)
			{
				_process.TryKill();
			}

			_remotePort = null;
			_hasProcessExited = true;
			_hasProcessFailed = true;

			try
			{
				Action<ProcessFaultReason> fn = OnFaultDetected;
				if (fn != null)
					fn(reason);
			}
			catch (Exception e)
			{
				Log.WarnFormat("OnFaultDetected threw an exception - ignoring it: {0}", e);
			}
		}

		private void EmitHostOutputWritten(string message)
		{
			Action<string> handler = OnHostOutputWritten;
			if (handler != null) handler(message);
		}

		private void StartHostProcess(out int pid)
		{
			try
			{
				if (!_process.Start())
					throw new SharpRemoteException(string.Format("Failed to start process {0}", _process.StartInfo.FileName));

				_hostedProcessId = pid = _process.Id;
			}
			catch (Win32Exception e)
			{
				switch (e.NativeErrorCode)
				{
					case Win32Error.ERROR_FILE_NOT_FOUND:

						Log.ErrorFormat("Unable to start host application '{0}' because the file cannot be found", _startInfo.FileName);

						throw new FileNotFoundException(e.Message, e);

					default:
						throw;
				}
			}
		}

		private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
		{
			string message = args.Data;
			EmitHostOutputWritten(message);

			switch (message)
			{
				case Constants.BootingMessage:
					_hostedProcessState = HostState.Booting;
					break;

				case Constants.ReadyMessage:
					_hostedProcessState = HostState.Ready;
					_waitHandle.Set();
					break;

				case Constants.ShutdownMessage:
					_hostedProcessState = HostState.None;
					break;

				case null:
					break;

				default:

					if (message.StartsWith(Constants.ExceptionMessage))
					{
						var encodedException = message.Substring(Constants.ExceptionMessage.Length);
						_startupException = DecodeException(encodedException);
						_waitHandle.Set();
					}
					else
					{
						int port;
						if (int.TryParse(message, out port))
							_remotePort = port;
					}
					break;
			}
		}

		internal static class Constants
		{
			public const string ExceptionMessage = "exception ";
			public const string BootingMessage = "booting";
			public const string ReadyMessage = "ready";
			public const string ShutdownMessage = "goodbye";
		}

		internal static Exception DecodeException(string encodedException)
		{
			using (var stream = new MemoryStream(Convert.FromBase64String(encodedException)))
			using (var reader = new BinaryReader(stream))
			{
				var actualException = AbstractEndPoint.ReadException(reader);
				return actualException;
			}
		}
	}
}