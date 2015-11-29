using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.CodeGeneration.Remoting;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using SharpRemote.Tasks;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Base class for any <see cref="IRemotingEndPoint" /> implementation that used an underlying
	///     <see cref="Socket" /> implementation
	/// </summary>
	public abstract class AbstractSocketRemotingEndPoint
		: AbstractEndPoint
		  , IRemotingEndPoint
		  , IEndPointChannel
	{
		private const ulong ServerLatencyServantId = ulong.MaxValue - 1;
		private const ulong ServerHeartbeatServantId = ulong.MaxValue - 2;
		private const ulong ClientLatencyServantId = ulong.MaxValue - 3;
		private const ulong ClientHeartbeatServantId = ulong.MaxValue - 4;

		private const string AuthenticationChallenge = "auth challenge";
		private const string AuthenticationResponse = "auth response";
		private const string AuthenticationVerification = "auth verification";
		private const string AuthenticationFinished = "auth finished";

		private const string AuthenticationRequiredMessage = "Authentication required";
		private const string NoAuthenticationRequiredMessage = "No Authentication required";
		private const string AuthenticationResponseMessage = "Authentication";
		private const string AuthenticationFailedMessage = "Authentication failed";
		private const string AuthenticationSucceedMessage = "Authentication succeeded";
		protected const string HandshakeSucceedMessage = "Handshake succeeded";

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#region Authentication

		private readonly IAuthenticator _clientAuthenticator;
		private readonly IAuthenticator _serverAuthenticator;

		#endregion

		#region Code Generation

		private readonly AssemblyBuilder _assembly;
		private readonly ModuleBuilder _module;
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;

		#endregion

		private readonly string _name;

		private readonly Serializer _serializer;
		private readonly object _syncRoot;
		private EndPointDisconnectReason? _disconnectReason;
		private bool _isDisposed;
		private long _nextRpcId;
		private Socket _socket;

		#region Statistics

		private readonly GrainIdGenerator _idGenerator;
		private long _numBytesReceived;
		private long _numBytesSent;
		private long _numCallsAnswered;
		private long _numCallsInvoked;

		/// <summary>
		///     The total amount of bytes that have been sent over the underlying socket.
		/// </summary>
		public long NumBytesSent
		{
			get { return Interlocked.Read(ref _numBytesSent); }
		}

		/// <summary>
		///     The total amount of bytes that have been received over the underlying socket.
		/// </summary>
		public long NumBytesReceived
		{
			get { return Interlocked.Read(ref _numBytesReceived); }
		}

		/// <summary>
		///     The total amount of remote procedure calls that have been invoked from this end.
		/// </summary>
		public long NumCallsInvoked
		{
			get { return Interlocked.Read(ref _numCallsInvoked); }
		}

		/// <summary>
		///     The total amount of remote procedure calls that have been invoked from the other end.
		/// </summary>
		public long NumCallsAnswered
		{
			get { return Interlocked.Read(ref _numCallsAnswered); }
		}

		#endregion

		#region Proxies / Servants

		private readonly Dictionary<ulong, WeakReference<IProxy>> _proxiesById;
		private readonly Dictionary<ulong, IServant> _servantsById;
		private readonly WeakKeyDictionary<object, IServant> _servantsBySubject;

		#endregion

		#region Method Invocation

		private readonly EndPointSettings _endpointSettings;
		private readonly PendingMethodsQueue _pendingMethodCalls;
		private readonly Dictionary<long, MethodInvocation> _pendingMethodInvocations;
		protected CancellationTokenSource CancellationTokenSource;

		#endregion

		#region Garbage Collection

		private long _numProxiesCollected;
		private long _numServantsCollected;
		private readonly Stopwatch _garbageCollectionTime;
		private readonly Timer _garbageCollectionTimer;

		#region Latency Measurements

		private readonly Latency _localLatency;
		private readonly ILatency _remoteLatency;
		private LatencyMonitor _latencyMonitor;

		#endregion

		#region Heartbeat

		private readonly Heartbeat _localHeartbeat;
		private readonly IHeartbeat _remoteHeartbeat;
		private readonly HeartbeatSettings _heartbeatSettings;
		private readonly LatencySettings _latencySettings;
		private HeartbeatMonitor _heartbeatMonitor;
		private bool _isDisposing;
		private DateTime _lastRead;

		#endregion

		#endregion

		/// <summary>
		/// The total number of <see cref="IProxy"/>s that have been removed from this endpoint because
		/// they're no longer used.
		/// </summary>
		public long NumProxiesCollected
		{
			get { return _numProxiesCollected; }
		}

		/// <summary>
		/// The total number of <see cref="IServant"/>s that have been removed from this endpoint because
		/// their subjects have been collected by the GC.
		/// </summary>
		public long NumServantsCollected
		{
			get { return _numServantsCollected; }
		}

		/// <summary>
		/// The total amount of time this endpoint spent collecting garbage.
		/// </summary>
		public TimeSpan GarbageCollectionTime
		{
			get { return _garbageCollectionTime.Elapsed; }
		}

		internal AbstractSocketRemotingEndPoint(GrainIdGenerator idGenerator,
		                                        string name,
		                                        EndPointType type,
		                                        IAuthenticator clientAuthenticator = null,
		                                        IAuthenticator serverAuthenticator = null,
		                                        ITypeResolver customTypeResolver = null,
		                                        Serializer serializer = null,
		                                        HeartbeatSettings heartbeatSettings = null,
		                                        LatencySettings latencySettings = null,
		                                        EndPointSettings endPointSettings = null)
		{
			if (idGenerator == null) throw new ArgumentNullException("idGenerator");
			if (heartbeatSettings != null)
			{
				if (heartbeatSettings.Interval <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("heartbeatSettings.Interval", "The heartbeat interval must be greater than zero");
				if (heartbeatSettings.SkippedHeartbeatThreshold <= 0)
					throw new ArgumentOutOfRangeException("heartbeatSettings.SkippedHeartbeatThreshold", "The skipped heartbeat threshold must be greater than zero");
			}

			_idGenerator = idGenerator;
			_name = name ?? "<Unnamed>";
			_syncRoot = new object();

			_servantsById = new Dictionary<ulong, IServant>();
			_servantsBySubject = new WeakKeyDictionary<object, IServant>();

			_proxiesById = new Dictionary<ulong, WeakReference<IProxy>>();

			if (serializer == null)
			{
				var assemblyName = new AssemblyName("SharpRemote.GeneratedCode");
				_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
				string moduleName = assemblyName.Name + ".dll";
				_module = _assembly.DefineDynamicModule(moduleName);

				_serializer = new Serializer(_module, customTypeResolver);
			}
			else
			{
				_module = serializer.Module;
				_serializer = serializer;
			}

			_servantCreator = new ServantCreator(_module, _serializer, this, this);
			_proxyCreator = new ProxyCreator(_module, _serializer, this, this);

			_endpointSettings = endPointSettings ?? new EndPointSettings();
			_pendingMethodCalls = new PendingMethodsQueue(_endpointSettings.MaxConcurrentCalls);
			_pendingMethodInvocations = new Dictionary<long, MethodInvocation>();

			_clientAuthenticator = clientAuthenticator;
			_serverAuthenticator = serverAuthenticator;

			_garbageCollectionTime = new Stopwatch();
			_garbageCollectionTimer = new Timer(CollectGarbage, null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));

			_localHeartbeat = new Heartbeat();
			_localLatency = new Latency();
			switch (type)
			{
				case EndPointType.Client:
					CreateServant<IHeartbeat>(ClientHeartbeatServantId, _localHeartbeat);
					_remoteHeartbeat = CreateProxy<IHeartbeat>(ServerHeartbeatServantId);

					CreateServant<ILatency>(ClientLatencyServantId, _localLatency);
					_remoteLatency = CreateProxy<ILatency>(ServerLatencyServantId);
					break;

				case EndPointType.Server:
					CreateServant<IHeartbeat>(ServerHeartbeatServantId, _localHeartbeat);
					_remoteHeartbeat = CreateProxy<IHeartbeat>(ClientHeartbeatServantId);

					CreateServant<ILatency>(ServerLatencyServantId, _localLatency);
					_remoteLatency = CreateProxy<ILatency>(ClientLatencyServantId);
					break;
			}

			_heartbeatSettings = heartbeatSettings ?? new HeartbeatSettings();
			_latencySettings = latencySettings ?? new LatencySettings();
		}

		/// <summary>
		/// The settings used for latency measurements.
		/// </summary>
		public LatencySettings LatencySettings
		{
			get { return _latencySettings; }
		}

		/// <summary>
		/// The settings used for the heartbeat mechanism.
		/// </summary>
		public HeartbeatSettings HeartbeatSettings
		{
			get { return _heartbeatSettings; }
		}

		/// <summary>
		/// The settings used for the endpoint itself (max. number of concurrent calls, etc...).
		/// </summary>
		public EndPointSettings EndPointSettings
		{
			get { return _endpointSettings; }
		}

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

			bool disconnecting = _heartbeatSettings.UseHeartbeatFailureDetection;
			var now = DateTime.Now;
			var difference = now - _lastRead;
			var heartbeatMonitor = _heartbeatMonitor;
			if (heartbeatMonitor != null && difference < _heartbeatMonitor.FailureInterval)
			{
				Log.WarnFormat(
					"Heartbeat monitor reported {0} missed heartbeats on the connection to '{1}', but the connection is merely heavily used",
					_heartbeatSettings.SkippedHeartbeatThreshold,
					InternalRemoteEndPoint);
			}
			else if (disconnecting)
			{
				Log.ErrorFormat("Heartbeat monitor detected a failure with the connection to '{0}': Disconnecting the endpoint",
				                InternalRemoteEndPoint);
				Disconnect(EndPointDisconnectReason.HeartbeatFailure);
			}
			else
			{
				Log.WarnFormat(
					"Heartbeat monitor reported a failure with the connection to '{0}': Ignoring as per heartbeat-settings...",
					InternalRemoteEndPoint);
			}
		}

		#region Reading from / Writing to socket

		protected void WriteLoop(object sock)
		{
			var args = (ThreadArgs) sock;
			Socket socket = args.Socket;
			CancellationToken token = args.Token;

			EndPointDisconnectReason reason;

			try
			{
				while (true)
				{
					if (token.IsCancellationRequested)
					{
						reason = EndPointDisconnectReason.RequestedByEndPoint;
						break;
					}

					int messageLength;
					byte[] message = _pendingMethodCalls.TakePendingWrite(token, out messageLength);

					if (message == null)
					{
						reason = EndPointDisconnectReason.RequestedByEndPoint;
						break;
					}

					SocketError error;
					if (!SynchronizedWrite(socket, message, messageLength, out error))
					{
						reason = EndPointDisconnectReason.WriteFailure;
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				reason = EndPointDisconnectReason.RequestedByEndPoint;
			}
			catch (Exception e)
			{
				reason = EndPointDisconnectReason.UnhandledException;
				Log.ErrorFormat("Caught exception while writing/handling messages: {0}", e);
			}

			Disconnect(reason);
		}

		protected void ReadLoop(object sock)
		{
			var args = (ThreadArgs) sock;
			Socket socket = args.Socket;

			EndPointDisconnectReason reason;

			try
			{
				var size = new byte[4];
				while (true)
				{
					SocketError err;
					if (!SynchronizedRead(socket, size, out err))
					{
						reason = EndPointDisconnectReason.ReadFailure;
						break;
					}

					int length = BitConverter.ToInt32(size, 0);
					if (length >= 8)
					{
						var buffer = new byte[length];
						if (!SynchronizedRead(socket, buffer, out err))
						{
							reason = EndPointDisconnectReason.ReadFailure;
							break;
						}

						var stream = new MemoryStream(buffer, false);
						var reader = new BinaryReader(stream);
						long rpcId = reader.ReadInt64();
						var type = (MessageType) reader.ReadByte();

						Interlocked.Add(ref _numBytesReceived, length + 4);
						_lastRead = DateTime.Now;

						EndPointDisconnectReason? r;
						if (!HandleMessage(rpcId, type, reader, out r))
						{
// ReSharper disable PossibleInvalidOperationException
							reason = (EndPointDisconnectReason) r;
// ReSharper restore PossibleInvalidOperationException

							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				reason = EndPointDisconnectReason.UnhandledException;
				Log.ErrorFormat("Caught exception while reading/handling messages: {0}", e);
			}

			Disconnect(reason);
		}

		private bool SynchronizedWrite(Socket socket, byte[] data, int length, out SocketError err)
		{
			lock (_syncRoot)
			{
				if (!socket.Connected)
				{
					err = SocketError.NotConnected;
					return false;
				}

				int written = socket.Send(data, 0, length, SocketFlags.None, out err);
				if (written != length || err != SocketError.Success || !socket.Connected)
				{
					Log.ErrorFormat("Error while writing to socket: {0} out of {1} written, method {2}, IsConnected: {3}", written,
					                data.Length, err, socket.Connected);
					return false;
				}

				return true;
			}
		}

		private bool SynchronizedRead(Socket socket, byte[] buffer, TimeSpan timeout, out SocketError err)
		{
			DateTime start = DateTime.Now;
			while (socket.Available < buffer.Length)
			{
				if (!socket.Connected)
				{
					err = SocketError.NotConnected;
					Log.DebugFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", 0,
					                buffer.Length, err, socket.Connected);
					return false;
				}

				TimeSpan remaining = timeout - (DateTime.Now - start);
				if (remaining <= TimeSpan.Zero)
				{
					err = SocketError.TimedOut;
					Log.DebugFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", 0,
					                buffer.Length, err, socket.Connected);
					return false;
				}

				var t = (int) (remaining.TotalMilliseconds*1000);
				if (!socket.Poll(t, SelectMode.SelectRead))
				{
					err = SocketError.TimedOut;
					Log.DebugFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", 0,
					                buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return SynchronizedRead(socket, buffer, out err);
		}

		private bool SynchronizedRead(Socket socket, byte[] buffer, out SocketError err)
		{
			err = SocketError.Success;

			int index = 0;
			int toRead;
			while ((toRead = buffer.Length - index) > 0)
			{
				int read = socket.Receive(buffer, index, toRead, SocketFlags.None, out err);
				index += read;

				if (err != SocketError.Success || read == 0 || !socket.Connected)
				{
					Log.DebugFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", read,
					                buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return true;
		}

		#endregion

		#region Timers

		//private readonly Stopwatch _createMessage = new Stopwatch();

		#endregion

		protected abstract EndPoint InternalLocalEndPoint { get; }
		protected abstract EndPoint InternalRemoteEndPoint { get; set; }

		protected object SyncRoot
		{
			get { return _syncRoot; }
		}

		protected Socket Socket
		{
			set { _socket = value; }
		}

		/// <summary>
		///     Tests if this object has been disposed of or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		/// <summary>
		///     Contains the reason why the socket was disconnected, or null if it wasn't disconnected / never established
		///     a connection.
		/// </summary>
		public EndPointDisconnectReason? DisconnectReason
		{
			get { return _disconnectReason; }
		}

		private void CollectGarbage(object unused)
		{
			_garbageCollectionTime.Start();
			try
			{
				RemoveUnusedServants();
				RemoveUnusedProxies();
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught exception during garbage collection: {0}", e);
			}
			finally
			{
				_garbageCollectionTime.Stop();
			}
		}

		private void RemoveUnusedServants()
		{
			lock (_servantsById)
			{
				var collectedServants = _servantsBySubject.Collect(true);
				if (collectedServants != null)
				{
					foreach (var servant in collectedServants)
					{
						_servantsById.Remove(servant.ObjectId);
					}

					_numServantsCollected += collectedServants.Count;
				}
			}
		}

		private void RemoveUnusedProxies()
		{
			lock (_proxiesById)
			{
				List<ulong> toRemove = null;

				foreach (var pair in _proxiesById)
				{
					IProxy proxy;
					if (!pair.Value.TryGetTarget(out proxy))
					{
						if (toRemove == null)
							toRemove = new List<ulong>();

						toRemove.Add(pair.Key);
					}
				}

				if (toRemove != null)
				{
					_numProxiesCollected += toRemove.Count;

					foreach (var key in toRemove)
					{
						_proxiesById.Remove(key);
					}
				}
			}
		}

		/// <summary>
		///     Returns all the proxies of this endpoint.
		///     Used for testing.
		/// </summary>
		internal IEnumerable<IProxy> Proxies
		{
			get
			{
				lock (_proxiesById)
				{
					var aliveProxies = new List<IProxy>();

					foreach (var pair in _proxiesById)
					{
						IProxy proxy;
						if (pair.Value.TryGetTarget(out proxy))
						{
							aliveProxies.Add(proxy);
						}
					}

					return aliveProxies;
				}
			}
		}

		/// <summary>
		///     Returns all the servnats of this endpoint.
		///     Used for testing.
		/// </summary>
		internal IEnumerable<IServant> Servants
		{
			get
			{
				lock (_servantsById)
				{
					return _servantsById.Values.ToList();
				}
			}
		}

		public Task<MemoryStream> CallRemoteMethodAsync(ulong servantId,
		                                                string interfaceType,
		                                                string methodName,
		                                                MemoryStream arguments)
		{
			long rpcId = Interlocked.Increment(ref _nextRpcId);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0} to {1}: sending RPC #{2} to {3}.{4}",
				                InternalLocalEndPoint,
				                InternalRemoteEndPoint,
				                rpcId,
				                servantId,
				                methodName);
			}

			return CallRemoteMethodAsync(rpcId, servantId, interfaceType, methodName, arguments);
		}

		public MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			long rpcId = Interlocked.Increment(ref _nextRpcId);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0} to {1}: sending RPC #{2} to {3}.{4}",
				                InternalLocalEndPoint,
				                InternalRemoteEndPoint,
				                rpcId,
				                servantId,
				                methodName);
			}

			return CallRemoteMethod(rpcId, servantId, interfaceType, methodName, arguments);
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				_isDisposing = true;
				try
				{
					//_pendingWrites.Dispose();

					Disconnect();
					DisposeAdditional();
					_garbageCollectionTimer.Dispose();

					// Another thread could still be accessing this dictionary.
					// Therefore we need to guard this one against concurrent access...
					lock (_servantsById)
					{
						_servantsBySubject.Dispose();
						_servantsById.Clear();
					}

					_isDisposed = true;
				}
				finally
				{
					_isDisposing = false;
				}
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public bool IsConnected
		{
			get { return InternalRemoteEndPoint != null; }
		}

		public TimeSpan RoundtripTime
		{
			get
			{
				var monitor = _latencyMonitor;
				if (monitor != null)
					return monitor.RoundtripTime;

				return TimeSpan.Zero;
			}
		}

		public void Disconnect()
		{
			Disconnect(EndPointDisconnectReason.RequestedByEndPoint);
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			lock (_proxiesById)
			{
				var proxy = _proxyCreator.CreateProxy<T>(objectId);
				var grain = new WeakReference<IProxy>((IProxy) proxy);
				_proxiesById.Add(objectId, grain);
				return proxy;
			}
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			IProxy proxy;
			lock (_proxiesById)
			{
				WeakReference<IProxy> grain;
				if (!_proxiesById.TryGetValue(objectId, out grain) || !grain.TryGetTarget(out proxy))
					throw new ArgumentException(string.Format("No such proxy: {0}", objectId));
			}

			if (!(proxy is T))
				throw new ArgumentException(string.Format("The proxy '{0}', {1} is not related to interface: {2}",
				                                          objectId,
				                                          proxy.GetType().Name,
				                                          typeof (T).Name));

			return (T) proxy;
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Creating new servant (#{2}) '{0}' implementing '{1}'",
				                subject.GetType().FullName,
				                typeof (T).FullName,
				                objectId
					);
			}

			IServant servant = _servantCreator.CreateServant(objectId, subject);
			lock (_servantsById)
			{
				_servantsById.Add(objectId, servant);
				_servantsBySubject.Add(subject, servant);
			}
			return servant;
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			lock (_proxiesById)
			{
				IProxy proxy;
				WeakReference<IProxy> grain;
				if (!_proxiesById.TryGetValue(objectId, out grain))
				{
					// If the proxy doesn't exist, then we can simply create a new one...
					var value = _proxyCreator.CreateProxy<T>(objectId);
					grain = new WeakReference<IProxy>((IProxy)value);
					_proxiesById.Add(objectId, grain);
					return value;
				}
				if (!grain.TryGetTarget(out proxy))
				{
					// It's possible that the proxy did exist at one point, then was collected by the GC, but
					// our internal GC didn't have the time to remove that proxy from the dictionary yet, which
					// means that we have to *replace* the existing weak-reference with a new, living one
					var value = _proxyCreator.CreateProxy<T>(objectId);
					grain = new WeakReference<IProxy>(proxy);
					_proxiesById[objectId] = grain;
					return value;
				}

				return (T) proxy;
			}
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			lock (_servantsById)
			{
				IServant servant;
				if (!_servantsBySubject.TryGetValue(subject, out servant))
				{
					ulong nextId = _idGenerator.GetGrainId();
					servant = CreateServant(nextId, subject);
				}

				return servant;
			}
		}

		protected static bool IsFailure(EndPointDisconnectReason reason)
		{
			switch (reason)
			{
				case EndPointDisconnectReason.RequestedByEndPoint:
				case EndPointDisconnectReason.RequestedByRemotEndPoint:
					return false;

				default:
					return true;
			}
		}

		private Task<MemoryStream> CallRemoteMethodAsync(long rpcId,
		                                                 ulong servantId,
		                                                 string interfaceType,
		                                                 string methodName,
		                                                 MemoryStream arguments)
		{
			var taskSource = new TaskCompletionSource<MemoryStream>();
			Action<PendingMethodCall> onCallFinished = finishedCall =>
				{
					// TODO: We might want execute this portion in yet another task in order to not block the read-thread
					try
					{
						if (finishedCall.MessageType == MessageType.Return)
						{
							var stream = (MemoryStream) finishedCall.Reader.BaseStream;
							taskSource.SetResult(stream);
						}
						else if ((finishedCall.MessageType & MessageType.Exception) != 0)
						{
							var e = ReadException(finishedCall.Reader);
							LogRemoteMethodCallException(rpcId, servantId, interfaceType, methodName, e);
							taskSource.SetException(e);
						}
						else
						{
							taskSource.SetException(new NotImplementedException());
						}
					}
					finally
					{
						_pendingMethodCalls.Recycle(finishedCall);
					}
				};

			PendingMethodCall call;
			lock (_syncRoot)
			{
				if (!IsConnected)
					throw new NotConnectedException(_name);

				call = _pendingMethodCalls.Enqueue(servantId,
																	 interfaceType,
																	 methodName,
																	 arguments,
																	 rpcId,
																	 onCallFinished);
			}

			Interlocked.Add(ref _numBytesSent, call.MessageLength);
			Interlocked.Increment(ref _numCallsInvoked);

			return taskSource.Task;
		}

		private MemoryStream CallRemoteMethod(long rpcId, ulong servantId, string interfaceType, string methodName,
		                                      MemoryStream arguments)
		{
			PendingMethodCall call = null;
			try
			{
				lock (_syncRoot)
				{
					if (!IsConnected)
						throw new NotConnectedException(_name);

					call = _pendingMethodCalls.Enqueue(servantId,
													  interfaceType,
													  methodName,
													  arguments,
													  rpcId);
				}

				Interlocked.Add(ref _numBytesSent, call.MessageLength);
				Interlocked.Increment(ref _numCallsInvoked);

				call.Wait();

				if (call.MessageType == MessageType.Return)
				{
					return (MemoryStream) call.Reader.BaseStream;
				}
				else if ((call.MessageType & MessageType.Exception) != 0)
				{
					var e = ReadException(call.Reader);
					LogRemoteMethodCallException(rpcId, servantId, interfaceType, methodName, e);
					throw e;
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			finally
			{
				if (call != null)
				{
					_pendingMethodCalls.Recycle(call);
				}
			}
		}

		private void LogRemoteMethodCallException(long rpcId, ulong servantId, string interfaceType, string methodName, Exception exception)
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("RPC invocation #{0} on {1}.{2} (#{3}) threw: {4}",
												rpcId,
												interfaceType,
												methodName,
												servantId,
												exception);
			}
		}

		protected abstract void DisposeAdditional();

		/// <summary>
		/// Performs a "hard" disconnect as if a failure occured.
		/// Is used to implement certain unit-tests where the connection
		/// failed (cable disconnected, etc...).
		/// </summary>
		internal void DisconnectByFailure()
		{
			Disconnect(EndPointDisconnectReason.ReadFailure);
		}

		private void Disconnect(EndPointDisconnectReason reason)
		{
			EndPoint remoteEndPoint;
			Socket socket;
			bool hasDisconnected = false;
			bool emitOnFailure = false;

			lock (_syncRoot)
			{
				remoteEndPoint = InternalRemoteEndPoint;
				socket = _socket;

				InternalRemoteEndPoint = null;
				_socket = null;

				if (socket != null)
				{
					var heartbeatMonitor = _heartbeatMonitor;
					if (heartbeatMonitor != null)
					{
						heartbeatMonitor.OnFailure -= HeartbeatMonitorOnOnFailure;
						heartbeatMonitor.Stop();
						heartbeatMonitor.TryDispose();
						_heartbeatMonitor = null;
					}

					var latencyMonitor = _latencyMonitor;
					if (latencyMonitor != null)
					{
						latencyMonitor.Stop();
						latencyMonitor.TryDispose();
						_latencyMonitor = null;
					}

					hasDisconnected = true;
					_disconnectReason = reason;

					Log.InfoFormat("Disconnecting socket '{0}' from {1}: {2}", _name, InternalRemoteEndPoint, reason);

					CancellationTokenSource.Cancel();
					_pendingMethodCalls.CancelAllCalls();

					// If we are disconnecting because of a failure, then we don't notify the other end
					// and drop the connection immediately. Also there's no need to notify the other
					// end when it requested the disconnect
					if (!IsFailure(reason))
					{
						if (reason != EndPointDisconnectReason.RequestedByRemotEndPoint)
						{
							SendGoodbye(socket);
						}
					}
					else
					{
						emitOnFailure = true;
					}

					try
					{
						socket.Disconnect(false);
					}
					catch (SocketException)
					{
					}
					catch (NullReferenceException)
					{
						// I suspect that either I forgot to lock one method call on _socket
						// or there's a bug in its implementation - either way this method may
						// throw a NullReferenceException from inside Disconnect.
					}
				}
			}

			if (emitOnFailure)
			{
				EmitOnFailure(reason);
			}

			EmitOnDisconnected(hasDisconnected, remoteEndPoint);
		}

		private void EmitOnFailure(EndPointDisconnectReason reason)
		{
			Action<EndPointDisconnectReason> fn = OnFailure;
			if (fn != null)
			{
				try
				{
					fn(reason);
				}
				catch (Exception e)
				{
					Log.WarnFormat("The OnFailure event threw an exception, please don't do that: {0}", e);
				}
			}
		}

		private void SendGoodbye(Socket socket)
		{
			try
			{
				long rpcId = _nextRpcId++;
				const int messageSize = 9;

				using (var stream = new MemoryStream())
				using (var writer = new BinaryWriter(stream, Encoding.UTF8))
				{
					writer.Write(messageSize);
					writer.Write(rpcId);
					writer.Write((byte) MessageType.Goodbye);

					writer.Flush();
					stream.Position = 0;

					socket.Send(stream.GetBuffer(), 0, messageSize + 4, SocketFlags.None);
				}
			}
			catch (SocketException)
			{
			}
		}

		protected void FireOnConnected(EndPoint endPoint)
		{
			_heartbeatMonitor = new HeartbeatMonitor(_remoteHeartbeat,
			                                         Diagnostics.Debugger.Instance,
			                                         _heartbeatSettings);

			_heartbeatMonitor.OnFailure += HeartbeatMonitorOnOnFailure;
			_heartbeatMonitor.Start();

			_latencyMonitor = new LatencyMonitor(_remoteLatency, _latencySettings);
			_latencyMonitor.Start();

			var fn = OnConnected;
			if (fn != null)
			{
				try
				{
					fn(endPoint);
				}
				catch (Exception e)
				{
					Log.WarnFormat("The OnConnected event threw an exception, please don't do that: {0}", e);
				}
			}
		}

		private void EmitOnDisconnected(bool hasDisconnected, EndPoint remoteEndPoint)
		{
			var fn2 = OnDisconnected;
			if (hasDisconnected && fn2 != null)
			{
				try
				{
					fn2(remoteEndPoint);
				}
				catch (Exception e)
				{
					Log.WarnFormat("The OnConnected event threw an exception, please don't do that: {0}", e);
				}
			}
		}

		/// <summary>
		/// Is called when a connection with another <see cref="AbstractSocketRemotingEndPoint"/>
		/// is created.
		/// </summary>
		/// <remarks>
		/// The event is fired with the endpoint of the *other* <see cref="AbstractSocketRemotingEndPoint"/>.
		/// </remarks>
		public event Action<EndPoint> OnConnected;

		/// <summary>
		/// Is called when a connection with another <see cref="AbstractSocketRemotingEndPoint"/> is disconnected.
		/// </summary>
		public event Action<EndPoint> OnDisconnected;

		/// <summary>
		///     This event is invoked right before a socket is to be closed due to failure of:
		///     - the connection between endpoints
		///     - a failure of the remote process
		///     - a failure of SharpRemote
		///     - something else ;)
		/// </summary>
		public event Action<EndPointDisconnectReason> OnFailure;

		private bool HandleMessage(long rpcId, MessageType type, BinaryReader reader, out EndPointDisconnectReason? reason)
		{
			if (type == MessageType.Call)
			{
				Interlocked.Increment(ref _numCallsAnswered);
				HandleRequest(rpcId, reader);
			}
			else if ((type & MessageType.Return) != 0)
			{
				if (!HandleResponse(rpcId, type, reader))
				{
					Log.ErrorFormat("There is no pending RPC #{0}, disconnecting...", rpcId);
					reason = EndPointDisconnectReason.RpcInvalidResponse;
					return false;
				}
			}
			else if ((type & MessageType.Goodbye) != 0)
			{
				Log.InfoFormat("Connection about to be closed by the other side - disconnecting...");

				reason = EndPointDisconnectReason.RequestedByRemotEndPoint;
				return false;
			}
			else
			{
				throw new SystemException(string.Format("Unexpected message-type: {0}", type));
			}

			reason = null;
			return true;
		}

		private void DispatchMethodInvocation(long rpcId, IGrain grain, string typeName, string methodName,
		                                      BinaryReader reader)
		{
			if (!IsTypeSafe(grain.InterfaceType, typeName))
			{
				// If the call violated type-safety then we will immediately "throw" a TypeMismatchException.
				HandleTypeMismatch(rpcId, grain, typeName, methodName);
			}
			else
			{
				SerialTaskScheduler taskScheduler = grain.GetTaskScheduler(methodName);

				Action executeMethod = () =>
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("Starting RPC #{0}", rpcId);
					}

					try
					{
						Socket socket = _socket;
						if (socket == null)
						{
							if (Log.IsDebugEnabled)
								Log.DebugFormat("RPC #{0} interrupted because the socket was disconnected", rpcId);

							return;
						}

						var response = new MemoryStream();
						var writer = new BinaryWriter(response, Encoding.UTF8);
						try
						{
							WriteResponseHeader(rpcId, writer, MessageType.Return);
							grain.Invoke(methodName, reader, writer);
							PatchResponseMessageLength(response, writer);
						}
						catch (Exception e)
						{
							if (Log.IsErrorEnabled)
							{
								Log.ErrorFormat("Caught exception while executing RPC #{0} on {1}.{2} (#{3}): {4}",
								                rpcId,
								                typeName,
								                methodName,
								                grain.ObjectId,
								                e);
							}

							response.Position = 0;
							WriteResponseHeader(rpcId, writer, MessageType.Return | MessageType.Exception);
							WriteException(writer, e);
							PatchResponseMessageLength(response, writer);
						}

						var responseLength = (int) response.Length;
						byte[] data = response.GetBuffer();

						SocketError err;
						
						if (!SynchronizedWrite(socket, data, responseLength, out err))
						{
							Log.ErrorFormat("Disconnecting socket due to error while writing response!");
							Disconnect();
						}
					}
					catch (Exception e)
					{
						Log.FatalFormat("Caught exception while dispatching method invocation, disconnecting: {0}", e);
						Disconnect(EndPointDisconnectReason.UnhandledException);
					}
					finally
					{
						if (Log.IsDebugEnabled)
						{
							Log.DebugFormat("Invocation of RPC #{0} finished", rpcId);
						}

						// Once we've created the task, we remember that there's a method invocation
						// that's yet to be executed (which tremendously helps debugging problems)
						lock (_pendingMethodInvocations)
						{
							_pendingMethodInvocations.Remove(rpcId);
						}
					}
				};

				// However if those 2 things don't throw, then we dispatch the rest of the method invocation
				// on the task dispatcher and be done with it here...
				Task task;
				TaskCompletionSource<int> completionSource;
				if (taskScheduler != null)
				{
					completionSource = new TaskCompletionSource<int>();
					task = completionSource.Task;
				}
				else
				{
					completionSource = null;
					task = new Task(executeMethod);
				}

				if (Log.IsDebugEnabled)
				{
					Log.DebugFormat("Queueing RPC #{0}", rpcId);
				}

				var methodInvocation = new MethodInvocation(rpcId, grain, methodName, task);
				lock (_pendingMethodInvocations)
				{
					_pendingMethodInvocations.Add(rpcId, methodInvocation);
				}

				// And then finally start the task to deserialize all method parameters, invoke the mehtod
				// and then seralize either the return value of the thrown exception...
				if (taskScheduler != null)
				{
					taskScheduler.QueueTask(executeMethod, completionSource);
				}
				else
				{
					task.Start(TaskScheduler.Default);
				}
			}
		}

		private void HandleTypeMismatch(long rpcId, IGrain grain, string typeName, string methodName)
		{
			var response = new MemoryStream();
			var writer = new BinaryWriter(response, Encoding.UTF8);
			response.Position = 0;
			WriteResponseHeader(rpcId, writer, MessageType.Return | MessageType.Exception);

			var e = new TypeMismatchException(
				string.Format(
					"There was a type mismatch when invoking RPC #{0} '{1}' on grain #{2}: Expected '{3}' but found '{4}",
					rpcId,
					methodName,
					grain.ObjectId,
					typeName,
					grain.InterfaceType.FullName));

			WriteException(writer, e);
			PatchResponseMessageLength(response, writer);

			var responseLength = (int) response.Length;
			byte[] data = response.GetBuffer();

			SocketError err;
			Socket socket = _socket;
			if (!SynchronizedWrite(socket, data, responseLength, out err))
			{
				Log.ErrorFormat("Disconnecting socket due to error while writing response!");
				Disconnect();
			}
		}

		private void HandleRequest(long rpcId, BinaryReader reader)
		{
			ulong servantId = reader.ReadUInt64();
			string typeName = reader.ReadString();
			string methodName = reader.ReadString();

			IServant servant;
			lock (_servantsById)
			{
				_servantsById.TryGetValue(servantId, out servant);
			}

			if (servant != null)
			{
				DispatchMethodInvocation(rpcId, servant, typeName, methodName, reader);
			}
			else
			{
				IProxy proxy;
				lock (_proxiesById)
				{
					WeakReference<IProxy> grain;
					if (_proxiesById.TryGetValue(servantId, out grain))
					{
						grain.TryGetTarget(out proxy);
					}
					else
					{
						proxy = null;
					}
				}

				if (proxy != null)
				{
					DispatchMethodInvocation(rpcId, proxy, typeName, methodName, reader);
				}
				else
				{
					throw new NoSuchServantException(servantId, typeName, methodName);
				}
			}
		}

		private static bool IsTypeSafe(Type getType, string typeName)
		{
			string actualTypeName = getType.FullName;
			return actualTypeName == typeName;
		}

		private static void PatchResponseMessageLength(MemoryStream response, BinaryWriter writer)
		{
			var bufferSize = (int) response.Length;
			int messageSize = bufferSize - 4;
			response.Position = 0;
			writer.Write(messageSize);
		}

		private static void WriteResponseHeader(long rpcId, BinaryWriter writer, MessageType type)
		{
			const int responseSizeStub = 0;
			writer.Write(responseSizeStub);
			writer.Write(rpcId);
			writer.Write((byte) type);
		}

		private bool HandleResponse(long rpcId, MessageType messageType, BinaryReader reader)
		{
			return _pendingMethodCalls.HandleResponse(rpcId, messageType, reader);
		}

		private bool TryReadMessage(Socket socket,
		                            TimeSpan timeout,
		                            string messageStep,
		                            out string messageType,
		                            out string message,
		                            out string error)
		{
			EndPoint remoteEndPoint = socket.RemoteEndPoint;
			var size = new byte[4];
			SocketError err;
			if (!SynchronizedRead(socket, size, timeout, out err))
			{
				messageType = null;
				message = null;
				error =
					string.Format(
						"EndPoint '{0}' did not receive '{1}' message from remote endpoint '{2}' in time: {3}s (error: {4})",
						Name,
						messageStep,
						remoteEndPoint,
						timeout.TotalSeconds,
						err);
				return false;
			}

			int length = BitConverter.ToInt32(size, 0);
			if (length < 0)
			{
				messageType = null;
				message = null;
				error = string.Format("The message received from remote endpoint '{0}' is malformatted",
				                      remoteEndPoint);
				return false;
			}

			var buffer = new byte[length];
			if (!SynchronizedRead(socket, buffer, timeout, out err))
			{
				messageType = null;
				message = null;
				error =
					string.Format(
						"EndPoint '{0}' did not receive '{1}' message from remote endpoint '{2}' in time: {3}s (error: {4})",
						Name,
						messageStep,
						remoteEndPoint,
						timeout.TotalSeconds,
						err);
				return false;
			}

			using (var reader = new BinaryReader(new MemoryStream(buffer)))
			{
				messageType = reader.ReadString();
				message = reader.ReadString();
			}

			error = null;
			return true;
		}

		protected void ReadMessage(Socket socket,
			TimeSpan timeout,
			string messageStep,
			out string messageType,
			out string message)
		{
			string error;
			if (!TryReadMessage(socket, timeout, messageStep, out messageType, out message, out error))
			{
				throw new HandshakeException(error);
			}
		}

		protected void WriteMessage(Socket socket,
		                            string messageType,
		                            string message = "")
		{
			string error;
			if (!TryWriteMessage(socket, messageType, message, out error))
			{
				throw new HandshakeException(error);
			}
		}

		private bool TryWriteMessage(Socket socket,
		                            string messageType,
		                            string message,
			out string error)
		{
			EndPoint remoteEndPoint = socket.RemoteEndPoint;
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				stream.Position = 4;
				writer.Write(messageType);
				writer.Write(message);
				writer.Flush();
				PatchResponseMessageLength(stream, writer);
				stream.Position = 0;

				SocketError err;
				if (!SynchronizedWrite(socket, stream.GetBuffer(), (int) stream.Length, out err))
				{
					error = string.Format("EndPoint '{0}' failed to send {1} to remote endpoint '{2}': {3}",
					                      Name,
					                      messageType,
					                      remoteEndPoint,
					                      err);
					return false;
				}
			}

			error = null;
			return true;
		}

		/// <summary>
		///     Performs the authentication between client & server (if necessary) from the server-side.
		/// </summary>
		/// <param name="socket"></param>
		protected void PerformIncomingHandshake(Socket socket)
		{
			EndPoint remoteEndPoint = socket.RemoteEndPoint;
			TimeSpan timeout = TimeSpan.FromMinutes(1);
			string messageType;
			string message;

			if (_clientAuthenticator != null)
			{
				// Upon accepting an incoming connection, we try to authenticate the client
				// by posing a challenge
				string challenge = _clientAuthenticator.CreateChallenge();
				Log.DebugFormat("Creating challenge '{0}' for endpoint '{1}'", challenge, remoteEndPoint);
				WriteMessage(socket, AuthenticationRequiredMessage, challenge);

				ReadMessage(socket, timeout, AuthenticationResponse, out messageType, out message);
				Log.DebugFormat("Received response '{0}' for challenge '{1}' from endpoint '{2}'",
				                message,
				                challenge,
				                remoteEndPoint);

				if (!_clientAuthenticator.Authenticate(challenge, message))
				{
					// Should the client fail the challenge, we tell him that,
					// but drop the connection immediately afterwards.
					WriteMessage(socket, AuthenticationFailedMessage);
					throw new AuthenticationException(string.Format("Endpoint '{0}' failed the authentication challenge",
					                                                remoteEndPoint));
				}

				WriteMessage(socket, AuthenticationSucceedMessage);
				Log.InfoFormat("Endpoint '{0}' successfully authenticated", remoteEndPoint);
			}
			else
			{
				WriteMessage(socket, NoAuthenticationRequiredMessage);
			}

			ReadMessage(socket, timeout, AuthenticationChallenge, out messageType, out message);
			if (messageType == AuthenticationRequiredMessage)
			{
				if (_serverAuthenticator == null)
					throw new AuthenticationRequiredException(string.Format("Endpoint '{0}' requires authentication", remoteEndPoint));

				string challenge = message;
				string response = _serverAuthenticator.CreateResponse(challenge);
				WriteMessage(socket, AuthenticationResponseMessage, response);

				// After having answered the challenge we wait for a successful response from the client.
				// If we failed the authentication, then 
				ReadMessage(socket, timeout, AuthenticationVerification, out messageType, out message);
				if (messageType != AuthenticationSucceedMessage)
					throw new AuthenticationException(string.Format("Failed to authenticate against endpoint '{0}'", remoteEndPoint));
			}
			else if (messageType != NoAuthenticationRequiredMessage)
			{
				throw new HandshakeException();
			}

			OnHandshakeSucceeded(socket);
			WriteMessage(socket, HandshakeSucceedMessage);
		}

		/// <summary>
		///     Performs the authentication between client & server (if necessary) from the client-side.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="timeout"></param>
		protected void PerformOutgoingHandshake(Socket socket, TimeSpan timeout)
		{
			ErrorType errorType;
			string error;
			if (!TryPerformOutgoingHandshake(socket, timeout, out errorType, out error))
			{
				switch (errorType)
				{
					case ErrorType.Handshake:
						throw new HandshakeException(error);

					case ErrorType.AuthenticationRequired:
						throw new AuthenticationRequiredException(error);

					default:
						throw new AuthenticationException(error);
				}
			}
		}

		protected enum ErrorType
		{
			None,

			Handshake,
			Authentication,
			AuthenticationRequired,
		}

		/// <summary>
		///     Performs the authentication between client & server (if necessary) from the client-side.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="timeout"></param>
		/// <param name="errorType"></param>
		/// <param name="error"></param>
		protected bool TryPerformOutgoingHandshake(Socket socket,
			TimeSpan timeout,
			out ErrorType errorType,
			out string error)
		{
			string messageType;
			string message;
			EndPoint remoteEndPoint = socket.RemoteEndPoint;

			if (!TryReadMessage(socket, timeout, AuthenticationChallenge,
				out messageType,
				out message,
				out error))
			{
				errorType =  ErrorType.Handshake;
				return false;
			}

			if (messageType == AuthenticationRequiredMessage)
			{
				if (_clientAuthenticator == null)
				{
					errorType = ErrorType.AuthenticationRequired;
					error = string.Format("Endpoint '{0}' requires authentication", remoteEndPoint);
					return false;
				}

				string challenge = message;
				// Upon establishing a connection, we try to authenticate the ourselves
				// against the server by answering his response.
				string response = _clientAuthenticator.CreateResponse(challenge);
				if (!TryWriteMessage(socket, AuthenticationResponseMessage, response, out error))
				{
					errorType = ErrorType.Handshake;
					return false;
				}

				// If we failed the authentication, a proper server will tell us so we can
				// forward this information to the caller.
				if (!TryReadMessage(socket, timeout, AuthenticationVerification,
					out messageType,
					out message,
					out error))
				{
					errorType = ErrorType.Handshake;
					return false;
				}

				if (messageType != AuthenticationSucceedMessage)
				{
					errorType = ErrorType.Authentication;
					error = string.Format("Failed to authenticate against endpoint '{0}'", remoteEndPoint);
					return false;
				}
			}
			else if (messageType != NoAuthenticationRequiredMessage)
			{
				errorType = ErrorType.Handshake;
				error =
					string.Format("EndPoint '{0}' sent unknown message '{1}: {2}', expected either {3} or {4}",
					              remoteEndPoint,
					              messageType,
					              message,
					              AuthenticationRequiredMessage,
					              NoAuthenticationRequiredMessage
						);
				return false;
			}

			if (_serverAuthenticator != null)
			{
				// After we've authenticated ourselves, it's time for the server to authenticate himself.
				// Let's send the challenge
				string challenge = _serverAuthenticator.CreateChallenge();
				if (!TryWriteMessage(socket, AuthenticationRequiredMessage, challenge, out error))
				{
					errorType = ErrorType.Handshake;
					return false;
				}

				if (!TryReadMessage(socket, timeout, AuthenticationResponse,
					out messageType,
					out message,
					out error))
				{
					errorType = ErrorType.Handshake;
					return false;
				}

				if (!_serverAuthenticator.Authenticate(challenge, message))
				{
					// Should the server fail to authenticate himself, then we tell him that end then abort
					// the connection...
					WriteMessage(socket, AuthenticationResponseMessage, AuthenticationFailedMessage);
					errorType = ErrorType.Authentication;
					error = string.Format("Remote endpoint '{0}' failed the authentication challenge",
					                      remoteEndPoint);
					return false;
				}

				if (!TryWriteMessage(socket, AuthenticationSucceedMessage, "", out error))
				{
					errorType = ErrorType.Handshake;
					return false;
				}

				Log.InfoFormat("Remote endpoint '{0}' successfully authenticated", remoteEndPoint);
			}
			else
			{
				WriteMessage(socket, NoAuthenticationRequiredMessage);
			}

			if (!TryReadMessage(socket, timeout, AuthenticationFinished, out messageType, out message, out error))
			{
				errorType = ErrorType.Handshake;
				return false;
			}

			if (messageType != HandshakeSucceedMessage)
			{
				errorType = ErrorType.Handshake;
				error =
					string.Format(
						"EndPoint '{0}' did not receive the correct response from remote endpoint '{1}': Expected '{2}' but received '{3}'",
						Name,
						remoteEndPoint,
						HandshakeSucceedMessage,
						messageType);

				return false;
			}

			OnHandshakeSucceeded(socket);

			errorType = ErrorType.Handshake;
			error = null;
			return true;
		}

		/// <summary>
		///     Is called when the handshake for the newly incoming message succeeds.
		/// </summary>
		/// <param name="socket"></param>
		protected abstract void OnHandshakeSucceeded(Socket socket);

		public override string ToString()
		{
			return _name;
		}

		protected sealed class ThreadArgs
		{
			public readonly Socket Socket;
			public readonly CancellationToken Token;

			public ThreadArgs(Socket socket, CancellationToken token)
			{
				Socket = socket;
				Token = token;
			}
		}
	}
}