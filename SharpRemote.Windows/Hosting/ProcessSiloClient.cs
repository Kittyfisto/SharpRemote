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
	///     <see cref="ISilo"/> implementation that allows client code to host objects in a remote
	/// process via <see cref="ProcessSiloServer"/>.
	/// </summary>
	/// <remarks>
	/// Can be used to host objects either in the SharpRemote.Host.exe or in a custom application
	/// of your choice by creating a <see cref="ProcessSiloServer"/> and calling <see cref="ProcessSiloServer.Run"/>.
	/// </remarks>
	public sealed class ProcessSiloClient
		: ISilo
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private const string SharpRemoteHost = "SharpRemote.Host.exe";

		private readonly SocketRemotingEndPoint _endPoint;
		private readonly Action<string> _hostOutputWritten;
		private readonly Process _process;
		private readonly ISubjectHost _subjectHost;
		private readonly ManualResetEvent _waitHandle;
		private HostState _hostState;

		private int? _remotePort;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="process"></param>
		/// <param name="options"></param>
		/// <param name="hostOutputWritten"></param>
		/// <param name="customTypeResolver">The type resolver, if any, responsible for resolving Type objects by their assembly qualified name</param>
		public ProcessSiloClient(
			string process = SharpRemoteHost,
			ProcessOptions options = ProcessOptions.HideConsole,
			Action<string> hostOutputWritten = null,
			ITypeResolver customTypeResolver = null
			)
		{
			if (process == null) throw new ArgumentNullException("process");

			_hostOutputWritten = hostOutputWritten;
			_endPoint = new SocketRemotingEndPoint(customTypeResolver: customTypeResolver);
			_subjectHost = _endPoint.CreateProxy<ISubjectHost>(Constants.SubjectHostId);
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
			get { return !_process.HasExited; }
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
		}

		internal static class Constants
		{
			/// <summary>
			///     The id of the grain that is used to instantiate further subjects.
			/// </summary>
			public const ulong SubjectHostId = 0;

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