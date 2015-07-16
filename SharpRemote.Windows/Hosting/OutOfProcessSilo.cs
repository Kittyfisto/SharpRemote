using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Reflection;
using System.Threading;
using SharpRemote.Extensions;
using log4net;

namespace SharpRemote.Hosting
{
	/// <summary>
	///     <see cref="ISilo"/> implementation that allows client code to host objects in another
	/// process via <see cref="OutOfProcessSiloServer"/>.
	/// </summary>
	/// <remarks>
	/// Can be used to host objects either in the SharpRemote.Host.exe or in a custom application
	/// of your choice by creating a <see cref="OutOfProcessSiloServer"/> and calling <see cref="OutOfProcessSiloServer.Run"/>.
	/// </remarks>
	/// <example>
	/// using (var silo = new OutOfProcessSilo())
	/// {
	///		var grain = silo.CreateGrain{IMyInterestingInterface}(typeof(MyRemoteType));
	///		grain.DoSomethingInteresting();
	/// }
	/// </example>
	public sealed class OutOfProcessSilo
		: ISilo
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private const string SharpRemoteHost = "SharpRemote.Host.exe";

		private readonly HeartbeatMonitor _monitor;
		private readonly SocketRemotingEndPoint _endPoint;
		private readonly Action<string> _hostOutputWritten;
		private readonly Process _process;
		private readonly ISubjectHost _subjectHost;
		private readonly ManualResetEvent _waitHandle;
		private HostState _hostState;

		private int? _remotePort;
		private bool _isDisposed;
		private bool _hasProcessExited;
		private bool _hasProcessFailed;

		/// <summary>
		/// Whether or not the remote process has exited
		/// (independent of failure or intentional exit).
		/// </summary>
		/// <remarks>
		/// Always the opposite of <see cref="IsProcessRunning"/>.
		/// </remarks>
		public bool HasProcessExited
		{
			get { return _hasProcessExited; }
		}

		/// <summary>
		/// Whether or not the process has failed.
		/// </summary>
		/// <remarks>
		/// False means that the process is either running or has exited on purpose.
		/// </remarks>
		public bool HasProcessFailed
		{
			get { return _hasProcessFailed; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="process"></param>
		/// <param name="options"></param>
		/// <param name="hostOutputWritten"></param>
		/// <param name="customTypeResolver">The type resolver, if any, responsible for resolving Type objects by their assembly qualified name</param>
		/// <param name="heartbeatSettings">The settings for heartbeat mechanism, if none are specified, then default settings are used</param>
		public OutOfProcessSilo(
			string process = SharpRemoteHost,
			ProcessOptions options = ProcessOptions.HideConsole,
			Action<string> hostOutputWritten = null,
			ITypeResolver customTypeResolver = null,
			HeartbeatSettings heartbeatSettings = null
			)
		{
			if (process == null) throw new ArgumentNullException("process");

			_hostOutputWritten = hostOutputWritten;

			_endPoint = new SocketRemotingEndPoint(customTypeResolver: customTypeResolver);
			_endPoint.OnFailure += EndPointOnOnFailure;

			_subjectHost = _endPoint.CreateProxy<ISubjectHost>(Constants.SubjectHostId);

			var heartbeat = _endPoint.CreateProxy<IHeartbeat>(Constants.HeartbeatId);
			_monitor = new HeartbeatMonitor(heartbeat, heartbeatSettings ?? new HeartbeatSettings());
			_monitor.OnFailure += MonitorOnOnFailure;

			_waitHandle = new ManualResetEvent(false);
			_hostState = HostState.BootPending;

			int parentPid = Process.GetCurrentProcess().Id;
			_process = new Process
				{
					StartInfo = new ProcessStartInfo(process)
						{
							Arguments = string.Format("{0}", parentPid),
							RedirectStandardOutput = true,
							UseShellExecute = false,
						}
				};
			switch (options)
			{
				case ProcessOptions.HideConsole:
					_process.StartInfo.CreateNoWindow = true;
					break;

				case ProcessOptions.ShowConsole:
					_process.StartInfo.CreateNoWindow = false;
					break;
			}

			_process.Exited += ProcessOnExited;
			_process.OutputDataReceived += ProcessOnOutputDataReceived;

			Log.InfoFormat("Starting host '{0}' for parent process (PID: {1})",
			               _process.StartInfo.FileName,
			               parentPid);

			if (!_process.Start())
				throw new NotImplementedException();

			_process.BeginOutputReadLine();

			if (!_waitHandle.WaitOne(Constants.ProcessReadyTimeout))
				throw new NotImplementedException();

			int? port = _remotePort;
			if (port == null)
				throw new NotImplementedException();

			_endPoint.Connect(new IPEndPoint(IPAddress.Loopback, port.Value), Constants.ConnectionTimeout);

			// After a successful connection, we can enable the heartbeat monitor so we're notified of failures
			_monitor.Start();
		}

		/// <summary>
		/// Is called when the endpoint reports a failure.
		/// </summary>
		private void EndPointOnOnFailure()
		{
			Log.ErrorFormat("SocketEndPoint detected a failure of the connection to the host process");
			HandleFailure();
		}

		/// <summary>
		/// Is called when the monitor detects a failure of the host process
		/// by:
		/// - lack of heartbeats
		/// - exception during heartbeats
		/// </summary>
		private void MonitorOnOnFailure()
		{
			Log.ErrorFormat("Heartbeat monitor detected a failure in the host process");
			HandleFailure();
		}

		private void HandleFailure()
		{
			// TODO: Think of a better way to handle failures thant to quit ;)
			_process.TryKill();
			_endPoint.Disconnect();
			_hasProcessFailed = true;
		}

		/// <summary>
		/// 
		/// </summary>
		public HostState HostState
		{
			get { return _hostState; }
		}

		/// <summary>
		/// 
		/// </summary>
		[Pure]
		public bool IsProcessRunning
		{
			get { return !_hasProcessExited; }
		}

		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		public void RegisterDefaultImplementation<TInterface, TImplementation>()
			where TImplementation : TInterface
			where TInterface : class
		{
			_subjectHost.RegisterDefaultImplementation(typeof(TImplementation), typeof (TInterface));
		}

		public TInterface CreateGrain<TInterface>(params object[] parameters) where TInterface : class
		{
			Type interfaceType = typeof(TInterface);
			ulong id = _subjectHost.CreateSubject3(interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters)
			where TInterface : class
		{
			Type interfaceType = typeof (TInterface);
			ulong id = _subjectHost.CreateSubject2(assemblyQualifiedTypeName, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters)
			where TInterface : class
		{
			Type interfaceType = typeof (TInterface);
			ulong id = _subjectHost.CreateSubject1(implementation, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public void Dispose()
		{
			_subjectHost.TryDispose();
			_endPoint.TryDispose();
			_process.TryKill();
			_process.TryDispose();
			_monitor.TryDispose();
			_hasProcessExited = true;
			_isDisposed = true;
		}

		private void EmitHostOutputWritten(string message)
		{
			Action<string> handler = _hostOutputWritten;
			if (handler != null) handler(message);
		}

		private void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs args)
		{
			string message = args.Data;
			EmitHostOutputWritten(message);
			switch (message)
			{
				case Constants.BootingMessage:
					_hostState = HostState.Booting;
					break;

				case Constants.ReadyMessage:
					_hostState = HostState.Ready;
					_waitHandle.Set();
					break;

				case Constants.ShutdownMessage:
					_hostState = HostState.None;
					break;

				default:
					int port;
					if (int.TryParse(message, out port))
						_remotePort = port;
					break;
			}
		}

		private void ProcessOnExited(object sender, EventArgs args)
		{
			_hasProcessExited = true;
		}

		internal static class Constants
		{
			/// <summary>
			///     The id of the grain that is used to instantiate further subjects.
			/// </summary>
			public const ulong SubjectHostId = ulong.MaxValue;

			/// <summary>
			/// The id of the grain that is used to detect whether or not the host process
			/// has failed.
			/// </summary>
			public const ulong HeartbeatId = ulong.MaxValue - 1;

			public const string BootingMessage = "booting";
			public const string ReadyMessage = "ready";
			public const string ShutdownMessage = "goodbye";

			/// <summary>
			///     The maximum amount of time the host process has to send the "ready" message before it is assumed
			///     to be dead / crashed / broken.
			/// </summary>
			public static readonly TimeSpan ProcessReadyTimeout = TimeSpan.FromSeconds(10);

			/// <summary>
			/// </summary>
			public static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(1);
		}
	}
}