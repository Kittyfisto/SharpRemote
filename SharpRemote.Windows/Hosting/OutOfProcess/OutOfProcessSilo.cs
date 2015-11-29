using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using SharpRemote.Hosting.OutOfProcess;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote.Hosting
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     <see cref="ISilo"/> implementation that allows client code to host objects in another
	/// process via <see cref="OutOfProcessSiloServer"/>.
	/// </summary>
	/// <remarks>
	/// Can be used to host objects either in the SharpRemote.Host.exe or in a custom application
	/// of your choice by creating a <see cref="OutOfProcessSiloServer"/> and calling <see cref="OutOfProcessSiloServer.Run()"/>.
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

		private readonly FailureSettings _failureSettings;
		private readonly ProcessWatchdog _process;
		private readonly SocketRemotingEndPointClient _endPoint;
		private readonly ISubjectHost _subjectHost;
		private readonly object _syncRoot;

		private readonly IFailureHandler _failureHandler;

		private ulong _nextObjectId;
		private bool _isDisposed;
		private bool _isDisposing;
		private Failure? _reason;

		/// <summary>
		/// This event is invoked whenever the host has written a complete line to its console.
		/// </summary>
		public event Action<string> OnHostOutputWritten;

		/// <summary>
		/// This event is invoked whenever the host process was successfully started, and a connection
		/// to it was established.
		/// </summary>
		public event Action OnHostStarted;

		/// <summary>
		/// Whether or not the process has failed.
		/// </summary>
		/// <remarks>
		/// False means that the process is either running or has exited on purpose.
		/// </remarks>
		public bool HasProcessFailed
		{
			get { return _process.HasProcessFailed; }
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
		/// <param name="serializer">The serializer used to serialize and deserialize values - if none is specifed then a new one is created</param>
		/// <param name="latencySettings">The settings for latency measurements, if none are specified, then default settings are used</param>
		/// <param name="postMortemSettings">The settings for the post mortem debugger of the host process, if none are specified then no post mortem debugging is performed</param>
		/// <param name="endPointSettings">The settings for the endpoint itself (max. number of concurrent calls, etc...)</param>
		/// <param name="failureSettings">The settings specifying when a failure is assumed to have occured in the host process - if none are specified, then defaults are used</param>
		/// <param name="failureHandler">The object responsible for deciding how failures are dealt with - if none is specified then a new <see cref="ZeroFailureToleranceStrategy"/> is used</param>
		/// <exception cref="ArgumentNullException">When <paramref name="process"/> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="process"/> is contains only whitespace</exception>
		public OutOfProcessSilo(
			string process = ProcessWatchdog.SharpRemoteHost,
			ProcessOptions options = ProcessOptions.HideConsole,
			ITypeResolver customTypeResolver = null,
			Serializer serializer = null,
			LatencySettings latencySettings = null,
			PostMortemSettings postMortemSettings = null,
			EndPointSettings endPointSettings = null,
			FailureSettings failureSettings = null,
			IFailureHandler failureHandler = null
			)
		{
			if (process == null) throw new ArgumentNullException("process");
			if (string.IsNullOrWhiteSpace(process)) throw new ArgumentException("process");
			if (postMortemSettings != null && !postMortemSettings.IsValid)
				throw new ArgumentException("postMortemSettings");
			if (failureSettings != null)
			{
				if (failureSettings.ProcessReadyTimeout <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("failureSettings", "ProcessReadyTimeout should be greater than zero");

				if (failureSettings.EndPointConnectTimeout <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("failureSettings", "EndPointConnectTimeout should be greater than zero");
			}

			_failureSettings = failureSettings ?? new FailureSettings();
			_failureHandler = failureHandler ?? new ZeroFailureToleranceStrategy();

			_endPoint = new SocketRemotingEndPointClient(customTypeResolver: customTypeResolver,
			                                             serializer: serializer,
			                                             heartbeatSettings: _failureSettings.HeartbeatSettings,
			                                             latencySettings: latencySettings,
			                                             endPointSettings: endPointSettings);
			_endPoint.OnFailure += EndPointOnOnFailure;

			_subjectHost = _endPoint.CreateProxy<ISubjectHost>(Constants.SubjectHostId);

			_syncRoot = new object();

			_process = new ProcessWatchdog(
				process,
				options,
				postMortemSettings
				);

			_process.OnFaultDetected += ProcessOnOnFaultDetected;
			_process.OnHostOutputWritten += EmitHostOutputWritten;
		}

		/// <summary>
		/// Starts this silo.
		/// </summary>
		/// <exception cref="FileNotFoundException">When the specified executable could not be found</exception>
		/// <exception cref="Win32Exception">When the </exception>
		/// <exception cref="HandshakeException">The handshake between this and the <see cref="OutOfProcessSiloServer"/> of the remote process failed</exception>
		/// <exception cref="SharpRemoteException"></exception>
		public void Start()
		{
			if (_process.IsProcessRunning)
				throw new InvalidOperationException();

			_process.Start();
			try
			{
				var port = _process.RemotePort;
				_endPoint.Connect(new IPEndPoint(IPAddress.Loopback, port.Value),
				                  _failureSettings.EndPointConnectTimeout);
			}
			catch (Exception e)
			{
				Log.WarnFormat("Caught unexpected exception after having started the host process (PID: {1}): {0}",
					e,
					_process.HostedProcessId);

				_process.TryKill();

				throw;
			}

			//
			// POINT OF NO FAILURE BELOW
			//

			Log.InfoFormat(
				"Host process '{0}' (PID: {1}) successfully started and connection to its endpoint at {2} established",
				_process.HostExecutableName,
				_process.HostedProcessId,
				_endPoint.RemoteEndPoint);

			try
			{
				var fn = OnHostStarted;
				if (fn != null)
					fn();
			}
			catch (Exception e)
			{
				Log.WarnFormat("The OnHostStarted event threw an exception, please don't do that: {0}", e);
			}
		}

		/// <summary>
		/// The current average round trip time or <see cref="TimeSpan.Zero"/> in
		/// case nothing was measured.
		/// </summary>
		public TimeSpan RoundtripTime
		{
			get { return _endPoint.RoundtripTime; }
		}

		/// <summary>
		/// Is called when the endpoint reports a failure.
		/// </summary>
		private void EndPointOnOnFailure(EndPointDisconnectReason reason)
		{
			lock (_syncRoot)
			{
				// If we're disposing this silo (or have disposed it alrady), then the heartbeat monitor
				// reported a failure that we caused intentionally (by killing the host process) and thus
				// this "failure" musn't be reported.
				if (_isDisposed || _isDisposing)
					return;
			}

			Log.ErrorFormat("SocketEndPoint detected a failure of the connection to the host process: {0}", reason);
			HandleFailure(reason);
		}

		private void ProcessOnOnFaultDetected(ProcessFaultReason processFaultReason)
		{
			switch (processFaultReason)
			{
				case ProcessFaultReason.HostProcessExited:
					HandleFailure(Failure.HostProcessExited, false);
					break;

				default:
				/*case ProcessFaultReason.UnhandledException:*/
					HandleFailure(Failure.UnhandledException, false);
					break;
			}
		}

		private void HandleFailure(EndPointDisconnectReason endPointReason)
		{
			Failure reason;
			switch (endPointReason)
			{
				case EndPointDisconnectReason.ReadFailure:
				case EndPointDisconnectReason.RpcInvalidResponse:
					reason = Failure.ConnectionFailure;
					break;

				case EndPointDisconnectReason.RequestedByEndPoint:
				case EndPointDisconnectReason.RequestedByRemotEndPoint:
					reason = Failure.ConnectionClosed;
					break;

				case EndPointDisconnectReason.HeartbeatFailure:
					reason = Failure.HeartbeatFailure;
					break;

				// ReSharper disable RedundantCaseLabel
				case EndPointDisconnectReason.UnhandledException:
				// ReSharper restore RedundantCaseLabel
				default:
					reason = Failure.UnhandledException;
					break;
			}

			HandleFailure(reason, dueToEndPoint: true);
		}

		private void HandleFailure(Failure failure, bool dueToEndPoint)
		{
			lock (_syncRoot)
			{
				if (_isDisposed || _isDisposing)
					return;

				if (_reason != null)
					return;

				_reason = failure;
			}

			var decision = Decision.Stop;
			try
			{
				var handlerResolution = _failureHandler.OnFailure(failure);
				if (handlerResolution != null)
					decision = handlerResolution.Value;
			}
			catch (Exception e)
			{
				Log.WarnFormat("OnFaultDetected threw an exception - ignoring it: {0}", e);
			}

			if (failure != Failure.HostProcessExited)
			{
				_process.TryKill();
			}

			// We don't want to call disconnect in case this method is executing because 
			// of an endpoint failure - because we're called from the endpoint's Disconnect method.
			// Calling disconnect again would overwrite the disconnect failure...
			if (!dueToEndPoint)
			{
				_endPoint.Disconnect();
			}

			var resolution = ResolveFailure(failure, decision);

			try
			{
				_failureHandler.OnResolutionFinished(failure, decision, resolution);
			}
			catch (Exception e)
			{
				Log.WarnFormat("IFailureHandler.OnResolutionFinished threw an exception - ignoring it: {0}", e);
			}
		}

		/// <summary>
		/// Actually tries to resolve the failure (if possible).
		/// </summary>
		/// <param name="failure"></param>
		/// <param name="decision"></param>
		/// <returns></returns>
		private Resolution ResolveFailure(Failure failure, Decision decision)
		{
			switch (decision)
			{
				case Decision.Stop: //< No need to do anything further...
					return Resolution.Stopped;

				case Decision.RestartHost:
					try
					{
						// This is a really bad hack but because we don't use GUIDs (yet) to identify
						// objects, 
						Start();
						return Resolution.Restarted;
					}
					catch (Exception e)
					{
						Log.FatalFormat("Tried to resolve the failure '{0}' by restarting the host - but this failed as well - we have to give up: {1}",
						                failure,
						                e);

						try
						{
							_failureHandler.OnResolutionFailed(failure, decision, e);
						}
						catch (Exception ex)
						{
							Log.WarnFormat("IFailureHandler.OnResolutionFailed threw an exception - ignoring it: {0}", ex);
						}

						return Resolution.Stopped;
					}

				default:
					Log.WarnFormat("Unknown decision '{0}' - interpreting it as '{1}'",
					               decision,
					               Decision.Stop);
					return Resolution.Stopped;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public HostState HostState
		{
			get { return _process.HostedProcessState; }
		}

		/// <summary>
		/// 
		/// </summary>
		[Pure]
		public bool IsProcessRunning
		{
			get { return _process.IsProcessRunning; }
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
				return _process.HostedProcessId;
			}
		}

		/// <summary>
		/// The total amount of time this endpoint spent collecting garbage.
		/// </summary>
		public TimeSpan GarbageCollectionTime
		{
			get { return _endPoint.GarbageCollectionTime; }
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

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			Type interfaceType = typeof(TInterface);
			_subjectHost.CreateSubject3(objectId, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
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

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			Type interfaceType = typeof (TInterface);
			_subjectHost.CreateSubject2(objectId, assemblyQualifiedTypeName, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
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

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			Type interfaceType = typeof (TInterface);
			_subjectHost.CreateSubject1(objectId, implementation, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
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

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			Type interfaceType = typeof(TInterface);
			_subjectHost.CreateSubject1(objectId, typeof(TImplementation), interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
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

			if (!HasProcessFailed)
			{
				_subjectHost.TryDispose();
			}

			_endPoint.TryDispose();
			_process.TryKill();
			_process.TryDispose();

			lock (_syncRoot)
			{
				_isDisposed = true;
				_isDisposing = false;
			}
		}

		private void EmitHostOutputWritten(string message)
		{
			Action<string> handler = OnHostOutputWritten;
			if (handler != null) handler(message);
		}

		internal static class Constants
		{
			/// <summary>
			///     The id of the grain that is used to instantiate further subjects.
			/// </summary>
			public const ulong SubjectHostId = ulong.MaxValue;
		}
	}
}