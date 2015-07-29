using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using SharpRemote.Exceptions;
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

		private readonly HeartbeatMonitor _heartbeatMonitor;
		private readonly LatencyMonitor _latencyMonitor;
		private readonly SocketRemotingEndPointClient _endPoint;
		private readonly ISubjectHost _subjectHost;
		private readonly ManualResetEvent _waitHandle;
		private readonly object _syncRoot;

		private Process _process;
		private HostState _hostState;

		private int? _remotePort;
		private bool _hasProcessExited;
		private bool _hasProcessFailed;
		private readonly int _parentPid;
		private readonly ProcessStartInfo _startInfo;
		private int? _hostProcessId;

		private bool _isDisposed;
		private bool _isDisposing;
		private OutOfProcessSiloFaultReason? _reason;

		/// <summary>
		/// This event is invoked whenever the host has written a complete line to its console.
		/// </summary>
		public event Action<string> HostOutputWritten;

		/// <summary>
		/// Is invoked when a fault in the remote process has been detected and is invoked prior to handling
		/// this failure.
		/// </summary>
		public event Action<OutOfProcessSiloFaultReason> OnFaultDetected;

		/// <summary>
		/// Is invoked when a fault in the remote process has been detected an handled.
		/// The parameters contain both the original reason and how its been handled.
		/// </summary>
		public event Action<OutOfProcessSiloFaultReason, OutOfProcessSiloFaultHandling> OnFaultHandled;

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

		#region Statistics

		/// <summary>
		/// The total amount of bytes that have been sent over the underlying socket endpoint.
		/// </summary>
		public long NumBytesSent
		{
			get { return _endPoint.NumBytesSent; }
		}

		/// <summary>
		/// The total amount of bytes that have been received over the underlying endpoint.
		/// </summary>
		public long NumBytesReceived
		{
			get { return _endPoint.NumBytesReceived; }
		}

		/// <summary>
		/// The total amount of remote procedure calls that have been invoked from this end.
		/// </summary>
		public long NumCallsInvoked
		{
			get { return _endPoint.NumCallsInvoked; }
		}

		/// <summary>
		/// The total amount of remote procedure calls that have been invoked from the other end.
		/// </summary>
		public long NumCallsAnswered
		{
			get { return _endPoint.NumCallsAnswered; }
		}

		#endregion

		/// <summary>
		/// Initializes a new instance of this silo with the specified options.
		/// The given host process will only be started once <see cref="Start"/> is called.
		/// </summary>
		/// <param name="process"></param>
		/// <param name="options"></param>
		/// <param name="customTypeResolver">The type resolver, if any, responsible for resolving Type objects by their assembly qualified name</param>
		/// <param name="heartbeatSettings">The settings for heartbeat mechanism, if none are specified, then default settings are used</param>
		/// <param name="latencySettings">The settings for latency measurements, if none are specified, then default settings are used</param>
		/// <exception cref="ArgumentNullException">When <paramref name="process"/> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="process"/> is contains only whitespace</exception>
		public OutOfProcessSilo(
			string process = SharpRemoteHost,
			ProcessOptions options = ProcessOptions.HideConsole,
			ITypeResolver customTypeResolver = null,
			HeartbeatSettings heartbeatSettings = null,
			LatencySettings latencySettings = null
			)
		{
			if (process == null) throw new ArgumentNullException("process");
			if (string.IsNullOrWhiteSpace(process)) throw new ArgumentException("process");

			_endPoint = new SocketRemotingEndPointClient(customTypeResolver: customTypeResolver);
			_endPoint.OnFailure += EndPointOnOnFailure;

			_subjectHost = _endPoint.CreateProxy<ISubjectHost>(Constants.SubjectHostId);

			var heartbeat = _endPoint.CreateProxy<IHeartbeat>(Constants.HeartbeatId);
			_heartbeatMonitor = new HeartbeatMonitor(heartbeat, heartbeatSettings ?? new HeartbeatSettings());
			_heartbeatMonitor.OnFailure += HeartbeatMonitorOnOnFailure;

			var latency = _endPoint.CreateProxy<ILatency>(Constants.LatencyProbeId);
			_latencyMonitor = new LatencyMonitor(latency, latencySettings ?? new LatencySettings());

			_waitHandle = new ManualResetEvent(false);
			_hostState = HostState.BootPending;
			_syncRoot = new object();

			_parentPid = Process.GetCurrentProcess().Id;
			_startInfo = new ProcessStartInfo(process)
				{
					Arguments = string.Format("{0}", _parentPid),
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
		/// Starts this silo 
		/// </summary>
		/// <exception cref="FileNotFoundException">When the specified executable could not be found</exception>
		/// <exception cref="Win32Exception">When the </exception>
		/// <exception cref="HandshakeException">The handshake between this and the <see cref="OutOfProcessSiloServer"/> of the remote process failed</exception>
		/// <exception cref="SharpRemoteException"></exception>
		public void Start()
		{
			_process = new Process
			{
				StartInfo = _startInfo,
				EnableRaisingEvents = true,
			};

			_process.Exited += ProcessOnExited;
			_process.OutputDataReceived += ProcessOnOutputDataReceived;

			Log.DebugFormat("Starting host '{0}' for parent process (PID: {1})",
							  _startInfo.FileName,
							  _parentPid);

			StartHostProcess();
			try
			{
				_hasProcessExited = false;

				_process.BeginOutputReadLine();

				if (!_waitHandle.WaitOne(Constants.ProcessReadyTimeout))
					throw new HandshakeException(string.Format("Process {0} failed to communicate used port number in time ({1}s)",
															   _process.StartInfo.FileName,
															   Constants.ProcessReadyTimeout.TotalSeconds));

				int? port = _remotePort;
				if (port == null)
					throw new HandshakeException(string.Format("Process {0} sent the ready signal, but failed to communicate the used port number",
															   _process.StartInfo.FileName));

				_endPoint.Connect(new IPEndPoint(IPAddress.Loopback, port.Value), Constants.ConnectionTimeout);

				// After a successful connection, we can enable the heartbeat monitor so we're notified of failures
				_heartbeatMonitor.Start();
				_latencyMonitor.Start();
			}
			catch (Exception e)
			{
				Log.WarnFormat("Caught unexpected exception after having started the host process (PID: {1}): {0}",
					e,
					_hostProcessId);

				_process.TryKill();
				_process.TryDispose();
				_process = null;

				throw;
			}

			Log.InfoFormat("Host '{0}' (PID: {1}) successfully started and connection to {2} established",
							  _process.StartInfo.FileName,
							  _process.Id,
							  _endPoint.RemoteEndPoint);
		}

		/// <summary>
		/// The current average round trip time or <see cref="TimeSpan.Zero"/> in
		/// case nothing was measured.
		/// </summary>
		public TimeSpan RoundtripTime
		{
			get { return _latencyMonitor.RoundTripTime; }
		}

		private void StartHostProcess()
		{
			try
			{
				if (!_process.Start())
					throw new SharpRemoteException(string.Format("Failed to start process {0}", _process.StartInfo.FileName));

				_hostProcessId = _process.Id;
			}
			catch (Win32Exception e)
			{
				switch ((Win32Error) e.NativeErrorCode)
				{
					case Win32Error.ERROR_FILE_NOT_FOUND:

						Log.ErrorFormat("Unable to start host process '{0}' because the file cannot be found", _startInfo.FileName);

						throw new FileNotFoundException(e.Message, e);

					default:
						throw;
				}
			}
		}

		/// <summary>
		/// Is called when the endpoint reports a failure.
		/// </summary>
		private void EndPointOnOnFailure(EndPointDisconnectReason reason)
		{
			Log.ErrorFormat("SocketEndPoint detected a failure of the connection to the host process: {0}", reason);
			HandleFailure(reason);
		}

		/// <summary>
		/// Is called when the monitor detects a failure of the host process
		/// by:
		/// - lack of heartbeats
		/// - exception during heartbeats
		/// </summary>
		private void HeartbeatMonitorOnOnFailure()
		{
			lock (_syncRoot)
			{
				// If we're disposing this silo (or have disposed it alrady), then the heartbeat monitor
				// reported a failure that we caused intentionally (by killing the host process) and thus
				// this "failure" musn't be reported.
				if (_isDisposed || _isDisposing)
					return;
			}

			Log.ErrorFormat("Heartbeat monitor detected a failure in the host process (PID: {0})", _hostProcessId);
			HandleFailure(null);
		}

		private void HandleFailure(EndPointDisconnectReason? endPointReason)
		{
			OutOfProcessSiloFaultReason reason;
			if (endPointReason != null)
			{
				switch (endPointReason.Value)
				{
					case EndPointDisconnectReason.ReadFailure:
					case EndPointDisconnectReason.RpcInvalidResponse:
						reason = OutOfProcessSiloFaultReason.ConnectionFailure;
						break;

					case EndPointDisconnectReason.RequestedByEndPoint:
					case EndPointDisconnectReason.RequestedByRemotEndPoint:
						reason = OutOfProcessSiloFaultReason.ConnectionClosed;
						break;

					// ReSharper disable RedundantCaseLabel
					case EndPointDisconnectReason.UnhandledException:
					// ReSharper restore RedundantCaseLabel
					default:
						reason = OutOfProcessSiloFaultReason.UnhandledException;
						break;
				}
			}
			else
			{
				reason = OutOfProcessSiloFaultReason.HeartbeatFailure;
			}

			HandleFailure(reason, dueToEndPoint: true);
		}

		private void HandleFailure(OutOfProcessSiloFaultReason reason, bool dueToEndPoint)
		{
			lock (_syncRoot)
			{
				if (_isDisposed || _isDisposing)
					return;

				if (_reason != null)
					return;

				_reason = reason;
			}

			try
			{
				var fn = OnFaultDetected;
				if (fn != null)
					fn(reason);
			}
			catch (Exception e)
			{
				Log.WarnFormat("OnFaultDetected threw an exception - ignoring it: {0}", e);
			}

			// TODO: Think of a better way to handle failures thant to quit ;)
			if (reason != OutOfProcessSiloFaultReason.HostProcessExited)
			{
				_process.TryKill();
			}

			_hasProcessExited = true;
			_hasProcessFailed = true;

			// We don't want to call disconnect in case this method is executing because 
			// of an endpoint failure - because we're called from the endpoint's Disconnect method.
			// Calling disconnect again would overwrite the disconnect reason...
			if (!dueToEndPoint)
			{
				_endPoint.Disconnect();
			}

			try
			{
				var fn = OnFaultHandled;
				if (fn != null)
					fn(reason, OutOfProcessSiloFaultHandling.Shutdown);
			}
			catch (Exception e)
			{
				Log.WarnFormat("OnFaultResolved threw an exception - ignoring it: {0}", e);
			}
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

		/// <summary>
		/// The process-id of the host process, or null, if it's not running.
		/// </summary>
		public int? HostProcessId
		{
			get
			{
				return _hostProcessId;
			}
		}

		public void RegisterDefaultImplementation<TInterface, TImplementation>()
			where TImplementation : TInterface
			where TInterface : class
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Registering default implementation '{0}' for interface '{1}'",
					typeof(TImplementation).FullName,
					typeof(TInterface).FullName);
			}

			_subjectHost.RegisterDefaultImplementation(typeof(TImplementation), typeof (TInterface));
		}

		public TInterface CreateGrain<TInterface>(params object[] parameters) where TInterface : class
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Creating grain using the registered default implementation for interface '{0}'", typeof(TInterface).FullName);
			}

			Type interfaceType = typeof(TInterface);
			ulong id = _subjectHost.CreateSubject3(interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters)
			where TInterface : class
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Creating grain of type '{0}' implementing interface '{1}'",
				                assemblyQualifiedTypeName,
				                typeof (TInterface).FullName);
			}

			Type interfaceType = typeof (TInterface);
			ulong id = _subjectHost.CreateSubject2(assemblyQualifiedTypeName, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters)
			where TInterface : class
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Creatign grain of type '{0}' implementing interface '{1}'",
				                implementation.FullName,
				                typeof (TInterface).FullName);
			}

			Type interfaceType = typeof (TInterface);
			ulong id = _subjectHost.CreateSubject1(implementation, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		public TInterface CreateGrain<TInterface, TImplementation>(params object[] parameters) where TInterface : class where TImplementation : TInterface
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Creatign grain of type '{0}' implementing interface '{1}'",
								typeof(TImplementation).FullName,
								typeof(TInterface).FullName);
			}

			Type interfaceType = typeof(TInterface);
			ulong id = _subjectHost.CreateSubject1(typeof(TImplementation), interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(id);
			return proxy;
		}

		/// <summary>
		///     Creates and registers an object that implements the given interface <typeparamref name="T" />.
		///     Calls to properties / methods of the given interface are marshalled to connected endpoint, if an appropriate
		///     servant of the same interface an <paramref name="objectId" /> has been created using <see cref="CreateServant{T}" />.
		/// </summary>
		/// <remarks>
		///     A proxy can be created independent from its servant and the order in which both are created is unimportant, for as long
		///     as no interface methods / properties are invoked.
		/// </remarks>
		/// <remarks>
		///     Every method / property on the given object is now capable of throwing an additional set of exceptions, in addition
		///     to whatever exceptions any implementation already throws:
		///     - <see cref="NoSuchServantException" />: There's no servant with the id of the proxy and therefore no subject on which the method could possibly be executed
		///     - <see cref="NotConnectedException" />: At the time of calling the proxy's method, no connection to a remote end point was available
		///     - <see cref="ConnectionLostException" />: The method call was cancelled because the connection between proxy and servant was interrupted / lost / disconnected
		///     - <see cref="UnserializableException" />: The remote method was executed, threw an exception, but the exception could not be serialized
		/// </remarks>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When there already exists a proxy of id <paramref name="objectId" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     When <typeparamref name="T" /> does not refer to an interface.
		/// </exception>
		public T CreateProxy<T>(ulong objectId) where T : class
		{
			return _endPoint.CreateProxy<T>(objectId);
		}

		/// <summary>
		///     Creates and registers an object for the given subject <paramref name="subject" /> and invokes its methods, when they
		///     have been called on the corresponding proxy.
		/// </summary>
		/// <remarks>
		///     A servant can be created independent from any proxy and the order in which both are created is unimportant, for as long
		///     as no interface methods / properties are invoked.
		/// </remarks>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <param name="subject"></param>
		/// <returns></returns>
		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			return _endPoint.CreateServant(objectId, subject);
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

			_heartbeatMonitor.TryDispose();
			_latencyMonitor.TryDispose();

			if (!HasProcessFailed)
			{
				_subjectHost.TryDispose();
			}

			_endPoint.TryDispose();
			_process.TryKill();
			_process.TryDispose();
			_hasProcessExited = true;

			lock (_syncRoot)
			{
				_isDisposed = true;
				_isDisposing = false;
			}
		}

		private void EmitHostOutputWritten(string message)
		{
			Action<string> handler = HostOutputWritten;
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
			HandleFailure(OutOfProcessSiloFaultReason.HostProcessExited, false);
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

			/// <summary>
			/// 
			/// </summary>
			public const ulong LatencyProbeId = ulong.MaxValue - 2;

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