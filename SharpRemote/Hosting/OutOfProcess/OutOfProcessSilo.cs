using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using log4net;
using SharpRemote.CodeGeneration;
using SharpRemote.Extensions;
using SharpRemote.Hosting.OutOfProcess;

// ReSharper disable CheckNamespace
namespace SharpRemote.Hosting
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     <see cref="ISilo" /> implementation that allows client code to host objects in another
	///     process via <see cref="OutOfProcessSiloServer" />.
	/// </summary>
	/// <remarks>
	///     Can be used to host objects either in the SharpRemote.Host.exe or in a custom application
	///     of your choice by creating a <see cref="OutOfProcessSiloServer" /> and calling
	///     <see cref="OutOfProcessSiloServer.Run()" />.
	/// </remarks>
	/// <example>
	///     using (var silo = new OutOfProcessSilo())
	///     {
	///     var grain = silo.CreateGrain{IMyInterestingInterface}(typeof(MyRemoteType));
	///     grain.DoSomethingInteresting();
	///     }
	/// </example>
	public sealed class OutOfProcessSilo
		: ISilo
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly SocketEndPoint _endPoint;
		private readonly ProcessWatchdog _process;
		private readonly OutOfProcessQueue _queue;
		private readonly ISubjectHost _subjectHost;
		private readonly object _syncRoot;
		private bool _isDisposing;

		private ulong _nextObjectId;

		/// <summary>
		///     Initializes a new instance of this silo with the specified options.
		///     The given host process will only be started once <see cref="Start" /> is called.
		/// </summary>
		/// <param name="process"></param>
		/// <param name="options"></param>
		/// <param name="codeGenerator">The code generator to create proxy and servant types</param>
		/// <param name="latencySettings">
		///     The settings for latency measurements, if none are specified, then default settings are
		///     used
		/// </param>
		/// <param name="endPointSettings">The settings for the endpoint itself (max. number of concurrent calls, etc...)</param>
		/// <param name="failureSettings">
		///     The settings specifying when a failure is assumed to have occured in the host process -
		///     if none are specified, then defaults are used
		/// </param>
		/// <param name="failureHandler">
		///     The object responsible for deciding how failures are dealt with - if none is specified
		///     then a new <see cref="ZeroFailureToleranceStrategy" /> is used
		/// </param>
		/// <param name="endPointName">The name of the endpoint - used in log messages to differentiate between different endpoints</param>
		/// <exception cref="ArgumentNullException">When <paramref name="process" /> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="process" /> is contains only whitespace</exception>
		public OutOfProcessSilo(
			string process = ProcessWatchdog.SharpRemoteHost,
			ProcessOptions options = ProcessOptions.HideConsole,
			ICodeGenerator codeGenerator = null,
			LatencySettings latencySettings = null,
			EndPointSettings endPointSettings = null,
			FailureSettings failureSettings = null,
			IFailureHandler failureHandler = null,
			string endPointName = null
		)
		{
			if (process == null) throw new ArgumentNullException(nameof(process));
			if (string.IsNullOrWhiteSpace(process)) throw new ArgumentException("process");
			if (failureSettings != null)
			{
				if (failureSettings.ProcessReadyTimeout <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(failureSettings), "ProcessReadyTimeout should be greater than zero");

				if (failureSettings.EndPointConnectTimeout <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(failureSettings),
						"EndPointConnectTimeout should be greater than zero");
			}

			failureSettings = failureSettings ?? new FailureSettings();
			failureHandler = failureHandler ?? new ZeroFailureToleranceStrategy();

			_endPoint = new SocketEndPoint(EndPointType.Client,
			                               endPointName,
			                               codeGenerator: codeGenerator,
			                               heartbeatSettings: failureSettings.HeartbeatSettings,
			                               latencySettings: latencySettings,
			                               endPointSettings: endPointSettings,
			                               waitUponReadWriteError: true);

			_subjectHost = _endPoint.CreateProxy<ISubjectHost>(Constants.SubjectHostId);

			_syncRoot = new object();

			_process = new ProcessWatchdog(
				process,
				options
			);

			_process.OnHostOutputWritten += EmitHostOutputWritten;

			_queue = new OutOfProcessQueue(
				_process,
				_endPoint,
				failureHandler,
				failureSettings
			);
			_queue.OnHostStarted += QueueOnOnHostStarted;
		}

		/// <summary>
		///     Whether or not the process has failed.
		/// </summary>
		/// <remarks>
		///     False means that the process is either running or has exited on purpose.
		/// </remarks>
		public bool HasProcessFailed => _process.HasProcessFailed;

		/// <summary>
		///     The current average round trip time or <see cref="TimeSpan.Zero" /> in
		///     case nothing was measured.
		/// </summary>
		public TimeSpan RoundtripTime => _endPoint.RoundtripTime;

		/// <summary>
		///     Returns a more precise state (than <see cref="IsProcessRunning" />) the process managed by this silo
		///     is currently in.
		/// </summary>
		public HostState HostState => _process.HostedProcessState;

		/// <summary>
		///     Returns true if the process managed by this silo is currently running, false otherwise.
		/// </summary>
		[Pure]
		public bool IsProcessRunning => _process.IsProcessRunning;

		/// <summary>
		///     Returns true if the process managed by this silo is currently running and the connection to it is healthy,
		///     false otherwise.
		/// </summary>
		[Pure]
		public bool IsConnected => _endPoint.IsConnected;

		/// <summary>
		///     The process-id of the host process, or null, if it's not running.
		/// </summary>
		public int? HostProcessId => _process.HostedProcessId;

		/// <summary>
		///     The total amount of time this endpoint spent collecting garbage.
		/// </summary>
		public TimeSpan GarbageCollectionTime => _endPoint.TotalGarbageCollectionTime;

		/// <inheritdoc />
		public bool IsDisposed { get; private set; }

		/// <inheritdoc />
		public void RegisterDefaultImplementation<TInterface, TImplementation>()
			where TImplementation : TInterface
			where TInterface : class
		{
			if (Log.IsDebugEnabled)
				Log.DebugFormat("Registering default implementation '{0}' for interface '{1}'",
					typeof(TImplementation).FullName,
					typeof(TInterface).FullName);

			_subjectHost.RegisterDefaultImplementation(typeof(TImplementation), typeof(TInterface));
		}

		/// <inheritdoc />
		public TInterface CreateGrain<TInterface>(params object[] parameters) where TInterface : class
		{
			if (Log.IsDebugEnabled)
				Log.DebugFormat("Creating grain using the registered default implementation for interface '{0}'",
					typeof(TInterface).FullName);

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			var interfaceType = typeof(TInterface);
			_subjectHost.CreateSubject3(objectId, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
			return proxy;
		}

		/// <inheritdoc />
		public TInterface CreateGrain<TInterface>(string assemblyQualifiedTypeName, params object[] parameters)
			where TInterface : class
		{
			if (Log.IsDebugEnabled)
				Log.DebugFormat("Creating grain of type '{0}' implementing interface '{1}'",
					assemblyQualifiedTypeName,
					typeof(TInterface).FullName);

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			var interfaceType = typeof(TInterface);
			_subjectHost.CreateSubject2(objectId, assemblyQualifiedTypeName, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
			return proxy;
		}

		/// <inheritdoc />
		public TInterface CreateGrain<TInterface>(Type implementation, params object[] parameters)
			where TInterface : class
		{
			if (Log.IsDebugEnabled)
				Log.DebugFormat("Creatign grain of type '{0}' implementing interface '{1}'",
					implementation.FullName,
					typeof(TInterface).FullName);

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			var interfaceType = typeof(TInterface);
			_subjectHost.CreateSubject1(objectId, implementation, interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
			return proxy;
		}

		/// <inheritdoc />
		public TInterface CreateGrain<TInterface, TImplementation>(params object[] parameters)
			where TInterface : class where TImplementation : TInterface
		{
			if (Log.IsDebugEnabled)
				Log.DebugFormat("Creatign grain of type '{0}' implementing interface '{1}'",
					typeof(TImplementation).FullName,
					typeof(TInterface).FullName);

			ulong objectId;
			lock (_syncRoot)
			{
				objectId = _nextObjectId++;
			}

			var interfaceType = typeof(TInterface);
			_subjectHost.CreateSubject1(objectId, typeof(TImplementation), interfaceType);
			var proxy = _endPoint.CreateProxy<TInterface>(objectId);
			return proxy;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (_syncRoot)
			{
				if (IsDisposed)
					return;

				if (_isDisposing)
					return;

				_isDisposing = true;
			}

			_queue.Dispose();

			// Do NOT use TryDispose here!!!
			TryDisposeSubjectHost();

			_endPoint.TryDispose();
			_process.TryKill();
			_process.TryDispose();

			lock (_syncRoot)
			{
				IsDisposed = true;
				_isDisposing = false;
			}
		}

		/// <summary>
		///     This event is invoked whenever the host has written a complete line to its console.
		/// </summary>
		public event Action<string> OnHostOutputWritten;

		/// <summary>
		///     This event is invoked whenever the host process was successfully started, and a connection
		///     to it was established.
		/// </summary>
		public event Action OnHostStarted;

		private void QueueOnOnHostStarted()
		{
			OnHostStarted?.Invoke();
		}

		/// <summary>
		///     Starts this silo.
		/// </summary>
		/// <exception cref="FileNotFoundException">When the specified executable could not be found</exception>
		/// <exception cref="Win32Exception">When the </exception>
		/// <exception cref="HandshakeException">
		///     The handshake between this and the <see cref="OutOfProcessSiloServer" /> of the
		///     remote process failed
		/// </exception>
		/// <exception cref="SharpRemoteException"></exception>
		/// <exception cref="AggregateException">
		///     The application was started multiple times, but failed to be started and connect
		///     every single time - examine <see cref="AggregateException.InnerExceptions" /> property
		/// </exception>
		public void Start()
		{
			_queue.Start().Wait();

			Log.InfoFormat(
				"Host process '{0}' (PID: {1}) successfully started",
				_process.HostExecutableName,
				_process.HostedProcessId);
		}

		/// <summary>
		///    Stops this silo.
		/// </summary>
		public void Stop()
		{
			var pid = _process.HostedProcessId;

			_queue.Stop().Wait();

			Log.InfoFormat(
			               "Host process '{0}' (PID: {1}) successfully stopped",
			               _process.HostExecutableName,
			               pid);
		}

		/// <summary>
		///     Creates and registers an object that implements the given interface <typeparamref name="T" />.
		///     Calls to properties / methods of the given interface are marshalled to connected endpoint, if an appropriate
		///     servant of the same interface an <paramref name="objectId" /> has been created using
		///     <see cref="CreateServant{T}" />.
		/// </summary>
		/// <remarks>
		///     A proxy can be created independent from its servant and the order in which both are created is unimportant, for as
		///     long
		///     as no interface methods / properties are invoked.
		/// </remarks>
		/// <remarks>
		///     Every method / property on the given object is now capable of throwing an additional set of exceptions, in addition
		///     to whatever exceptions any implementation already throws:
		///     - <see cref="NoSuchServantException" />: There's no servant with the id of the proxy and therefore no subject on
		///     which the method could possibly be executed
		///     - <see cref="NotConnectedException" />: At the time of calling the proxy's method, no connection to a remote end
		///     point was available
		///     - <see cref="ConnectionLostException" />: The method call was canceled because the connection between proxy and
		///     servant was interrupted / lost / disconnected
		///     - <see cref="UnserializableException" />: The remote method was executed, threw an exception, but the exception
		///     could not be serialized
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
		///     Creates and registers an object for the given subject <paramref name="subject" /> and invokes its methods, when
		///     they
		///     have been called on the corresponding proxy.
		/// </summary>
		/// <remarks>
		///     A servant can be created independent from any proxy and the order in which both are created is unimportant, for as
		///     long
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

		private void EmitHostOutputWritten(string message)
		{
			OnHostOutputWritten?.Invoke(message);
		}

		private void TryDisposeSubjectHost()
		{
			try
			{
				if (IsProcessRunning)
				{
					// This is an RPC call which throws
					// an exception in case the connection was dropped.
					_subjectHost.Dispose();
				}
			}
			catch (RemoteProcedureCallCanceledException e)
			{
				// HOWEVER, if the reason for the cancelled operation was a disconnect
				// AND we already know that the process isn't running anymore, then we don't need
				// to log an additional exception stacktrace...
				if (IsProcessRunning)
				{
					Log.WarnFormat("Caught exception while disposing '{0}': {1}", _subjectHost, e);
				}
				else
				{
					Log.DebugFormat("Caught exception while disposing '{0}': {1}", _subjectHost, e);
				}
			}
			catch (Exception e)
			{
				Log.WarnFormat("Caught exception while disposing '{0}': {1}", _subjectHost, e);
			}
		}

		internal static class Constants
		{
			/// <summary>
			///     The id of the grain that is used to instantiate further subjects.
			/// </summary>
			public const ulong SubjectHostId = ulong.MaxValue;
		}

		#region Statistics

		/// <summary>
		///     The total amount of bytes that have been sent over the underlying socket endpoint.
		/// </summary>
		public long NumBytesSent => _endPoint.NumBytesSent;

		/// <summary>
		///     The total amount of bytes that have been received over the underlying endpoint.
		/// </summary>
		public long NumBytesReceived => _endPoint.NumBytesReceived;

		/// <summary>
		///     The total amount of remote procedure calls that have been invoked from this end.
		/// </summary>
		public long NumCallsInvoked => _endPoint.NumCallsInvoked;

		/// <summary>
		///     The total amount of remote procedure calls that have been invoked from the other end.
		/// </summary>
		public long NumCallsAnswered => _endPoint.NumCallsAnswered;

		#endregion
	}
}