using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using SharpRemote.CodeGeneration;
using SharpRemote.EndPoints;
using SharpRemote.EndPoints.Sockets;
using SharpRemote.Extensions;
using SharpRemote.Tasks;
using Debugger = SharpRemote.Diagnostics.Debugger;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Base class for any remoting end point.
	/// </summary>
	public abstract class AbstractBinaryStreamEndPoint<TTransport>
		: AbstractEndPoint
		  , IRemotingEndPoint
		  , IEndPointChannel
		where TTransport : class, IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const ulong ServerLatencyServantId = ulong.MaxValue - 1;
		private const ulong ServerHeartbeatServantId = ulong.MaxValue - 2;
		private const ulong ClientLatencyServantId = ulong.MaxValue - 3;
		private const ulong ClientHeartbeatServantId = ulong.MaxValue - 4;

		private const string AuthenticationChallenge = "auth challenge";
		private const string AuthenticationResponse = "auth response";
		private const string AuthenticationVerification = "auth verification";
		private const string AuthenticationFinished = "auth finished";
		private const string EndPointBlocked = "endpoint blocked";

		internal const string AuthenticationRequiredMessage = "Authentication required";
		internal const string NoAuthenticationRequiredMessage = "No Authentication required";
		internal const string AuthenticationResponseMessage = "Authentication";
		internal const string AuthenticationFailedMessage = "Authentication failed";
		internal const string AuthenticationSucceedMessage = "Authentication succeeded";
		internal const string HandshakeSucceedMessage = "Handshake succeeded";

		#region Statistics

		private long _numBytesReceived;
		private long _numMessagesReceived;
		private long _numBytesSent;
		private long _numMessagesSent;
		private long _numCallsAnswered;
		private long _numCallsInvoked;
		
		/// <inheritdoc />
		public long NumBytesSent => Interlocked.Read(ref _numBytesSent);
		
		/// <inheritdoc />
		public long NumBytesReceived => Interlocked.Read(ref _numBytesReceived);
		
		/// <inheritdoc />
		public long NumMessagesSent => Interlocked.Read(ref _numMessagesSent);
		
		/// <inheritdoc />
		public long NumMessagesReceived => Interlocked.Read(ref _numMessagesReceived);
		
		/// <inheritdoc />
		public long NumCallsInvoked => Interlocked.Read(ref _numCallsInvoked);

		/// <inheritdoc />
		public long NumCallsAnswered => Interlocked.Read(ref _numCallsAnswered);

		/// <inheritdoc />
		public long NumPendingMethodCalls => _pendingMethodCalls.NumPendingCalls;

		/// <inheritdoc />
		public TimeSpan? AverageRoundTripTime => _latencyMonitor?.RoundtripTime;

		#endregion

		#region Proxies / Servants

		private readonly ProxyStorage _proxies;
		private readonly ServantStorage _servants;

		#endregion

		#region Method Invocation

		private readonly EndPointSettings _endpointSettings;
		private readonly PendingMethodsQueue _pendingMethodCalls;
		private readonly Dictionary<long, MethodInvocation> _pendingMethodInvocations;
		private CancellationTokenSource _cancellationTokenSource;

		#endregion

		#region Garbage Collection

		private readonly Stopwatch _garbageCollectionTime;
		private readonly Timer _garbageCollectionTimer;

		#endregion
		
		#region Heartbeat

		private readonly HeartbeatSettings _heartbeatSettings;
		private readonly LatencySettings _latencySettings;
		private readonly Heartbeat _localHeartbeat;
		private readonly IHeartbeat _remoteHeartbeat;
		private HeartbeatMonitor _heartbeatMonitor;
		private bool _isDisposing;
		private DateTime _lastRead;

		#endregion

		#region Statistics

		private readonly Latency _localLatency;
		private readonly ILatency _remoteLatency;
		private LatencyMonitor _latencyMonitor;
		private EndPointStatistics _statistics;

		#endregion

		#region Code Generation

		private readonly ICodeGenerator _codeGenerator;

		#endregion

		#region Authentication

		private readonly IAuthenticator _clientAuthenticator;
		private readonly IAuthenticator _serverAuthenticator;

		#endregion

		#region Reading / Writing

		private Thread _readThread;
		private Thread _writeThread;

		#endregion

		private int _previousConnectionId;
		private readonly string _name;
		private readonly object _syncRoot;
		private readonly bool _waitUponReadWriteError;
		private readonly EndPointType _type;

		private EndPointDisconnectReason? _disconnectReason;
		private bool _isDisposed;
		private long _nextRpcId;
		private TTransport _socket;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="idGenerator"></param>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="clientAuthenticator"></param>
		/// <param name="serverAuthenticator"></param>
		/// <param name="codeGenerator"></param>
		/// <param name="heartbeatSettings"></param>
		/// <param name="latencySettings"></param>
		/// <param name="endPointSettings"></param>
		/// <param name="waitUponReadWriteError"></param>
		protected AbstractBinaryStreamEndPoint(GrainIdGenerator idGenerator,
		                                      string name,
		                                      EndPointType type,
		                                      IAuthenticator clientAuthenticator,
		                                      IAuthenticator serverAuthenticator,
		                                      ICodeGenerator codeGenerator,
		                                      HeartbeatSettings heartbeatSettings,
		                                      LatencySettings latencySettings,
		                                      EndPointSettings endPointSettings,
		                                      bool waitUponReadWriteError = false)
		{
			if (idGenerator == null) throw new ArgumentNullException(nameof(idGenerator));
			if (heartbeatSettings != null)
			{
				if (heartbeatSettings.Interval <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("heartbeatSettings.Interval",
					                                      "The heartbeat interval must be greater than zero");
				if (heartbeatSettings.SkippedHeartbeatThreshold <= 0)
					throw new ArgumentOutOfRangeException("heartbeatSettings.SkippedHeartbeatThreshold",
					                                      "The skipped heartbeat threshold must be greater than zero");
			}

			_waitUponReadWriteError = waitUponReadWriteError;
			_previousConnectionId = 0;
			_type = type;
			_name = name ?? "Unnamed";
			_syncRoot = new object();

			_codeGenerator = codeGenerator ?? CodeGeneration.CodeGenerator.Default;
			_proxies = new ProxyStorage(this, this, _codeGenerator);
			_servants = new ServantStorage(this, this, idGenerator, _codeGenerator);

			_endpointSettings = endPointSettings ?? new EndPointSettings();
			_pendingMethodCalls = new PendingMethodsQueue(_name, _endpointSettings.MaxConcurrentCalls);
			_pendingMethodInvocations = new Dictionary<long, MethodInvocation>();

			_clientAuthenticator = clientAuthenticator;
			_serverAuthenticator = serverAuthenticator;

			_garbageCollectionTime = new Stopwatch();
			_garbageCollectionTimer = new Timer(CollectGarbage, null, TimeSpan.FromMilliseconds(100),
			                                    TimeSpan.FromMilliseconds(100));

			_localHeartbeat = new Heartbeat(Debugger.Instance, this, heartbeatSettings != null ? heartbeatSettings.ReportDebuggerAttached : true);
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

				default:
					throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(EndPointType));
			}

			_heartbeatSettings = heartbeatSettings ?? new HeartbeatSettings();
			_latencySettings = latencySettings ?? new LatencySettings();

			Log.DebugFormat("{0}: Created '{1}' endpoint", _name, type);
		}

		internal ICodeGenerator CodeGenerator => _codeGenerator;

		/// <summary>
		///     The socket used to communicate with the other endpoint.
		/// </summary>
		protected TTransport Socket
		{
			set { _socket = value; }
		}

		/// <summary>
		///     The endpoint-address of this endpoint.
		/// </summary>
		protected abstract EndPoint InternalLocalEndPoint { get; }

		/// <summary>
		///     The endpoint-address of this endpoint we're connected to, or null.
		/// </summary>
		protected abstract EndPoint InternalRemoteEndPoint { get; set; }

		/// <inheritdoc />
		public long NumProxiesCollected => _proxies.NumProxiesCollected;

		/// <inheritdoc />
		public long NumServantsCollected => _servants.NumServantsCollected;

		/// <inheritdoc />
		public TimeSpan TotalGarbageCollectionTime => _garbageCollectionTime.Elapsed;

		/// <inheritdoc />
		public LatencySettings LatencySettings => _latencySettings;

		/// <inheritdoc />
		public HeartbeatSettings HeartbeatSettings => _heartbeatSettings;

		/// <inheritdoc />
		public EndPointSettings EndPointSettings => _endpointSettings;

		/// <inheritdoc />
		public long NumPendingMethodInvocations
		{
			get
			{
				lock (_pendingMethodInvocations)
				{
					return _pendingMethodInvocations.Count;
				}
			}
		}

		/// <summary>
		///     Tests if this object has been disposed of or not.
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		///     Contains the reason why the endpoint was disconnected, or null if it wasn't disconnected / never established
		///     a connection.
		/// </summary>
		public EndPointDisconnectReason? DisconnectReason => _disconnectReason;

		/// <summary>
		///     The lock used to ensure that certain sections are not executed in parallel
		///     (mostly to do with connecting/disconnecting).
		/// </summary>
		protected object SyncRoot => _syncRoot;

		/// <summary>
		///     Returns all the proxies of this endpoint.
		///     Used for testing.
		/// </summary>
		public IEnumerable<IProxy> Proxies
		{
			get
			{
				return _proxies.Proxies;
			}
		}

		/// <summary>
		///     Returns all the servants of this endpoint.
		///     Used for testing.
		/// </summary>
		internal IEnumerable<IServant> Servants => _servants.Servants;

		/// <inheritdoc />
		public Task<MemoryStream> CallRemoteMethodAsync(ulong servantId,
		                                                string interfaceType,
		                                                string methodName,
		                                                MemoryStream arguments)
		{
			long rpcId = Interlocked.Increment(ref _nextRpcId);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0}: {1} to {2}, sending RPC #{3} to {4}.{5}",
				                Name,
				                InternalLocalEndPoint,
				                InternalRemoteEndPoint,
				                rpcId,
				                servantId,
				                methodName);
			}

			return CallRemoteMethodAsync(rpcId, servantId, interfaceType, methodName, arguments);
		}

		/// <inheritdoc />
		public MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			long rpcId = Interlocked.Increment(ref _nextRpcId);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0}: {1} to {2}, sending RPC #{3} to {4}.{5}",
				                Name,
				                InternalLocalEndPoint,
				                InternalRemoteEndPoint,
				                rpcId,
				                servantId,
				                methodName);
			}

			return CallRemoteMethod(rpcId, servantId, interfaceType, methodName, arguments);
		}

		/// <inheritdoc />
		public EndPoint LocalEndPoint => InternalLocalEndPoint;

		/// <inheritdoc />
		public EndPoint RemoteEndPoint => InternalRemoteEndPoint;

		/// <inheritdoc />
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
					_garbageCollectionTimer.TryDispose();
					_localHeartbeat.TryDispose();

					// Another thread could still be accessing this dictionary.
					// Therefore we need to guard this one against concurrent access...
					_servants.Dispose();

					_isDisposed = true;
				}
				finally
				{
					_isDisposing = false;
				}
			}
		}

		/// <inheritdoc />
		public string Name => _name;

		/// <inheritdoc />
		public bool IsConnected => InternalRemoteEndPoint != null;

		/// <inheritdoc />
		public ConnectionId CurrentConnectionId { get; protected set; }

		/// <inheritdoc />
		public TimeSpan RoundtripTime
		{
			get
			{
				LatencyMonitor monitor = _latencyMonitor;
				if (monitor != null)
					return monitor.RoundtripTime;

				return TimeSpan.Zero;
			}
		}

		/// <inheritdoc />
		public void Disconnect()
		{
			Disconnect(CurrentConnectionId, EndPointDisconnectReason.RequestedByEndPoint);
		}

		/// <inheritdoc />
		public T CreateProxy<T>(ulong objectId) where T : class
		{
			return _proxies.CreateProxy<T>(objectId);
		}

		/// <inheritdoc />
		public T GetProxy<T>(ulong objectId) where T : class
		{
			return _proxies.GetProxy<T>(objectId);
		}

		/// <inheritdoc />
		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			return _servants.CreateServant(objectId, subject);
		}

		/// <inheritdoc />
		public T RetrieveSubject<T>(ulong objectId) where T : class
		{
			return _servants.RetrieveSubject<T>(objectId);
		}

		/// <inheritdoc />
		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			return _proxies.GetExistingOrCreateNewProxy<T>(objectId);
		}

		/// <inheritdoc />
		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			return _servants.GetExistingOrCreateNewServant(subject);
		}

		/// <summary>
		///     Is called when a connection with another <see cref="AbstractBinaryStreamEndPoint{TTransport}" />
		///     is created.
		/// </summary>
		/// <remarks>
		///     The event is fired with the endpoint of the *other* <see cref="AbstractBinaryStreamEndPoint{TTransport}" />.
		/// </remarks>
		public event Action<EndPoint, ConnectionId> OnConnected;

		/// <summary>
		///     Is called when a connection with another <see cref="AbstractBinaryStreamEndPoint{TTransport}" /> is disconnected.
		/// </summary>
		public event Action<EndPoint, ConnectionId> OnDisconnected;

		/// <summary>
		///     This event is invoked right before an endpoint's connection is to be closed due to failure of:
		///     - the connection between endpoints
		///     - a failure of the remote process
		///     - a failure of SharpRemote
		///     - something else ;)
		/// </summary>
		public event Action<EndPointDisconnectReason, ConnectionId> OnFailure;

		private void HeartbeatMonitorOnOnFailure(ConnectionId currentConnectionId)
		{
			lock (_syncRoot)
			{
				// If we're disposing this silo (or have disposed it alrady), then the heartbeat monitor
				// reported a failure that we caused intentionally (by killing the host process) and thus
				// this "failure" musn't be reported.
				if (_isDisposed || _isDisposing)
					return;

				// We can safely ignore failures reported by the heartbeat monitor that are from any other
				// than the current connection.
				if (currentConnectionId != CurrentConnectionId)
					return;
			}

			bool disconnecting = _heartbeatSettings.UseHeartbeatFailureDetection;
			DateTime now = DateTime.Now;
			TimeSpan difference = now - _lastRead;
			HeartbeatMonitor heartbeatMonitor = _heartbeatMonitor;
			if (heartbeatMonitor != null && difference < _heartbeatMonitor.FailureInterval)
			{
				Log.WarnFormat(
					"{0}: Heartbeat monitor reported {1} missed heartbeats on the connection to '{2}', but the connection is merely heavily used",
					Name,
					_heartbeatSettings.SkippedHeartbeatThreshold,
					InternalRemoteEndPoint);
			}
			else if (disconnecting)
			{
				Disconnect(currentConnectionId, EndPointDisconnectReason.HeartbeatFailure);
			}
			else
			{
				Log.WarnFormat(
					"{0}: Heartbeat monitor reported a failure with the connection to '{1}': Ignoring as per heartbeat-settings...",
					Name,
					InternalRemoteEndPoint);
			}
		}

		private void CollectGarbage(object unused)
		{
			_garbageCollectionTime.Start();
			try
			{
				int numServantsRemoved = _servants.RemoveUnusedServants();
				int numProxiesRemoved = _proxies.RemoveUnusedProxies();

				if (numProxiesRemoved > 0)
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("{0}: Removed {1} proxies because they are no longer reachable",
						                Name,
						                numProxiesRemoved);
					}
				}

				if (numServantsRemoved > 0)
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("{0}: Removed {1} servants because they are no longer reachable",
						                Name,
						                numServantsRemoved);
					}
				}

				if (Log.IsDebugEnabled)
					Log.DebugFormat("{0}: Total GC time: {1}ms", Name, _garbageCollectionTime.ElapsedMilliseconds);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("{0}: Caught exception during garbage collection: {1}",
				                Name,
				                e);
			}
			finally
			{
				_garbageCollectionTime.Stop();
			}
		}

		/// <summary>
		///     Tests if the given disconnect reason indicates a failure of the connection
		///     or rather an intentional disconnect.
		/// </summary>
		/// <param name="reason"></param>
		/// <returns></returns>
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
							Exception e = ReadException(finishedCall.Reader);
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

			_pendingMethodCalls.Enqueue(servantId,
			                            interfaceType,
			                            methodName,
			                            arguments,
			                            rpcId,
			                            onCallFinished);
			Interlocked.Increment(ref _numCallsInvoked);

			return taskSource.Task;
		}

		private MemoryStream CallRemoteMethod(long rpcId, ulong servantId, string interfaceType, string methodName,
		                                      MemoryStream arguments)
		{
			PendingMethodCall call = null;
			try
			{
				call = _pendingMethodCalls.Enqueue(servantId,
				                                   interfaceType,
				                                   methodName,
				                                   arguments,
				                                   rpcId);

				Interlocked.Add(ref _numBytesSent, call.MessageLength);
				Interlocked.Increment(ref _numCallsInvoked);

				call.Wait();

				var messageType = call.MessageType;
				if (messageType == MessageType.Return)
				{
					return (MemoryStream) call.Reader.BaseStream;
				}
				else if ((messageType & MessageType.Exception) != 0)
				{
					Exception e = ReadException(call.Reader);
					LogRemoteMethodCallException(rpcId, servantId, interfaceType, methodName, e);
					throw e;
				}
				else
				{
					throw new NotImplementedException(string.Format("Unexpected message type: {0} ({1})",
					                                                messageType,
					                                                (int) messageType));
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

		private void LogRemoteMethodCallException(long rpcId, ulong servantId, string interfaceType, string methodName,
		                                          Exception exception)
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0}: RPC invocation #{1} on {2}.{3} (#{4}) threw: {5}",
				                Name,
				                rpcId,
				                interfaceType,
				                methodName,
				                servantId,
				                exception);
			}
		}

		/// <summary>
		///     Called when <see cref="Dispose" /> is called.
		/// </summary>
		protected abstract void DisposeAdditional();

		/// <summary>
		///     Performs a "hard" disconnect as if a failure occured.
		///     Is used to implement certain unit-tests where the connection
		///     failed (cable disconnected, etc...).
		/// </summary>
		internal void DisconnectByFailure()
		{
			Disconnect(CurrentConnectionId, EndPointDisconnectReason.Unknown);
		}

		private void Disconnect(ConnectionId currentConnectionId,
			                    EndPointDisconnectReason reason)
		{
			if (_waitUponReadWriteError && IsFailure(reason))
			{
				Log.DebugFormat("Disconnecting because of {0}, waiting for a little bit to find out of this was caused by a process crash...",
					reason);

				// This may seem odd, but we do have a problem in where the TCP connection
				// is disconnected BEFORE we receive and event that the hosting process has been
				// killed. This in turn causes us to report the wrong problem (the connection
				// was dropped, but it appeared because the bloody process crashed). By waiting
				// for a little bit, we correct the order of events most of the time...
				Thread.Sleep(100);

				// We've waited long enough. If a process crash/kill was responsible for this disconnect,
				// then Disconnect() will have already been called, causing this method to return without
				// having to do anything (see 10 lines below, the first return statement).
			}

			EndPoint remoteEndPoint;
			bool hasDisconnected = false;
			bool emitOnFailure = false;
			ConnectionId connectionId;

			lock (_syncRoot)
			{
				// We can safely ignore failures reported by the heartbeat monitor that are from any other
				// than the current connection.
				if (currentConnectionId != CurrentConnectionId)
					return;

				// We DON'T want to emit an error message when we are already disconnected. This is
				// because Disconnect() doesn't wait for the read/Write thread to stop and therefore
				// those threads are almost always reporting a "failure" afterwards.
				if (IsFailure(reason))
				{
					var explanation = CreateDisconnectExplanation(reason);
					var builder = new StringBuilder();
					builder.AppendFormat("{0}: Disconnecting EndPoint '{1}' from '{2}': {3}",
					                     Name,
					                     InternalLocalEndPoint,
					                     InternalRemoteEndPoint,
					                     explanation);
					Log.Error(builder);
				}

				remoteEndPoint = InternalRemoteEndPoint;
				var socket = _socket;

				InternalRemoteEndPoint = null;
				_pendingMethodCalls.IsConnected = false;
				_socket = null;

				connectionId = CurrentConnectionId;

				if (socket != null)
				{
					HeartbeatMonitor heartbeatMonitor = _heartbeatMonitor;
					if (heartbeatMonitor != null)
					{
						heartbeatMonitor.OnFailure -= HeartbeatMonitorOnOnFailure;
						heartbeatMonitor.Stop();
						heartbeatMonitor.TryDispose();
						_heartbeatMonitor = null;
					}

					_latencyMonitor?.TryDispose();
					_latencyMonitor = null;

					_statistics?.TryDispose();
					_statistics = null;

					hasDisconnected = true;
					_disconnectReason = reason;

					Log.InfoFormat("{0}: Disconnecting socket '{1}' from {2}: {3}",
					               Name,
					               InternalLocalEndPoint,
					               InternalRemoteEndPoint,
					               reason);

					_cancellationTokenSource.Cancel();
					_pendingMethodCalls.CancelAllCalls();

					ClearPendingMethodInvocations();
					ClearTransientProxies();

					// If we are disconnecting because of a failure, then we don't notify the other end
					// and drop the connection immediately. Also there's no need to notify the other
					// end when it requested the disconnect
					if (!IsFailure(reason))
					{
						if (reason != EndPointDisconnectReason.RequestedByRemotEndPoint)
						{
							// We don't want to wait anymore than 5 seconds to send the goodbye message.
							// If it didn't work then screw it, we'll disconnect anyways...
							SendGoodbye(socket, TimeSpan.FromSeconds(5));
						}
					}
					else
					{
						emitOnFailure = true;
					}

					try
					{
						DisconnectTransport(socket, false);
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
					DisposeAfterDisconnect(socket);
				}

				CurrentConnectionId = ConnectionId.None;
			}

			if (emitOnFailure)
			{
				EmitOnFailure(reason, connectionId);
			}

			EmitOnDisconnected(hasDisconnected, remoteEndPoint, connectionId);
		}

		#region Disconnect Error Messages

		/// <summary>
		///    Creates an explanation as to why the connection was disconnected.
		///    This is most likely due to external circumstances (such as the cabling having been removed,
		///    the other side crashing, etc..) and this method tries to find a way to explain that
		///    to a human in simple terms.
		/// </summary>
		/// <param name="reason"></param>
		/// <returns></returns>
		private string CreateDisconnectExplanation(EndPointDisconnectReason reason)
		{
			switch (reason)
			{
				case EndPointDisconnectReason.RpcDuplicateRequest:
					return
						"The connection was dropped because a request with the same RPC id than an alrady pending request was made.";

				case EndPointDisconnectReason.RpcInvalidResponse:
					return "The connection was dropped because a response to a non-existant pending RPC was received.";

				case EndPointDisconnectReason.UnhandledException:
					return "The connection was dropped because an unexpected exception within SharpRemote. Please report the exception to the developer at https://github.com/Kittyfisto/SharpRemote/issues/new.";

				case EndPointDisconnectReason.HeartbeatFailure:
					return "The connection was dropped because the remote peer failed to respond to heartbeats in time (the remote peer might be too overwhelmed to respond or the connection has been interrupted)";

				case EndPointDisconnectReason.ConnectionReset:
					return "The connection was reset by the remote peer (this is usually caused by the remote process exiting unexpectedly or crashing).";

				case EndPointDisconnectReason.ConnectionAborted:
					return "The connection was aborted by the underlying software on either this or the remote computer.";

				case EndPointDisconnectReason.ConnectionTimedOut:
					return "A read or write operation timed out (this is usually caused by the remote host no longer being reachable)";

				default:
					return "The connection was dropped for an unknown reason";
			}
		}

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="reuseSocket"></param>
		protected abstract void DisconnectTransport(TTransport socket, bool reuseSocket);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		protected abstract void DisposeAfterDisconnect(TTransport socket);

		/// <summary>
		/// 
		/// </summary>
		protected void ClearPendingMethodInvocations()
		{
			lock (_pendingMethodInvocations)
			{
				_pendingMethodInvocations.Clear();
			}
		}

		/// <summary>
		/// Removes all those proxies which were created because the other endpoint
		/// created (likely temporary) servants and sent them over the network.
		/// </summary>
		private void ClearTransientProxies()
		{
			try
			{
				var otherType = _type == EndPointType.Client ? EndPointType.Server : EndPointType.Client;
				var range = GrainIdGenerator.GetRangeFor(otherType);
				_proxies.RemoveProxiesInRange(range.Minimum, range.Maximum);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Unable to remove transient proxies: {0}", e);
			}
		}

		private void EmitOnFailure(EndPointDisconnectReason reason, ConnectionId connectionId)
		{
			Action<EndPointDisconnectReason, ConnectionId> fn = OnFailure;
			if (fn != null)
			{
				try
				{
					fn(reason, connectionId);
				}
				catch (Exception e)
				{
					Log.WarnFormat("{0}: The OnFailure event threw an exception, please don't do that: {1}",
					               Name,
					               e);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="remoteEndPoint"></param>
		/// <param name="connectionId"></param>
		protected void FireOnConnected(EndPoint remoteEndPoint, ConnectionId connectionId)
		{
			_heartbeatMonitor = new HeartbeatMonitor(_remoteHeartbeat,
			                                         Debugger.Instance,
			                                         _heartbeatSettings,
			                                         connectionId,
			                                         _name,
			                                         LocalEndPoint,
			                                         remoteEndPoint);

			_heartbeatMonitor.OnFailure += HeartbeatMonitorOnOnFailure;
			_heartbeatMonitor.Start();

			_latencyMonitor = new LatencyMonitor(_remoteLatency,
			                                     _latencySettings,
			                                     _name);
			_latencyMonitor.Start();

			_statistics = new EndPointStatistics(this);
			_statistics.Start();

			Action<EndPoint, ConnectionId> fn = OnConnected;
			if (fn != null)
			{
				try
				{
					fn(remoteEndPoint, connectionId);
				}
				catch (Exception e)
				{
					Log.WarnFormat("{0}: The OnConnected event threw an exception, please don't do that: {1}",
					               Name,
					               e);
				}
			}
		}

		private void EmitOnDisconnected(bool hasDisconnected, EndPoint remoteEndPoint, ConnectionId connectionId)
		{
			Action<EndPoint, ConnectionId> fn2 = OnDisconnected;
			if (hasDisconnected && fn2 != null)
			{
				try
				{
					fn2(remoteEndPoint, connectionId);
				}
				catch (Exception e)
				{
					Log.WarnFormat("{0}: The OnConnected event threw an exception, please don't do that: {1}",
					               Name,
					               e);
				}
			}
		}

		private bool HandleMessage(
			ConnectionId currentConnectionId,
			long rpcId,
			MessageType type,
			BinaryReader reader,
			out EndPointDisconnectReason? reason)
		{
			if (type == MessageType.Call)
			{
				Interlocked.Increment(ref _numCallsAnswered);
				return HandleRequest(currentConnectionId, rpcId, reader, out reason);
			}
			if ((type & MessageType.Return) != 0)
			{
				if (!HandleResponse(rpcId, type, reader))
				{
					// This is quite bad: Why did we get a response to an RPC that we did not send?
					// However it shouldn't cause us to immediately abort the connection. Instead
					// we log a sensible error and try to continue (we can rely on the heartbeat
					// mechanism to abort the connection when too many messages don't ping back).
					lock (_syncRoot)
					{
						if (InternalRemoteEndPoint != null)
						{
							Log.ErrorFormat("{0}: There is no pending RPC #{1}. This shouldn't happend and might be indicative of a bug. Please contact the library author if this issue persists.",
							                Name,
							                rpcId);
						}
					}

					reason = null;
					return true;
				}
			}
			else if ((type & MessageType.Goodbye) != 0)
			{
				Log.InfoFormat("{0}: Connection about to be closed by the other side - disconnecting...", Name);

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

		private bool DispatchMethodInvocation(
			ConnectionId connectionId,
			long rpcId,
			IGrain grain,
			string typeName,
			string methodName,
			BinaryReader reader,
			out EndPointDisconnectReason? reason)
		{
			if (!IsTypeSafe(grain.InterfaceType, typeName))
			{
				// If the call violated type-safety then we will immediately "throw" a TypeMismatchException.
				HandleTypeMismatch(connectionId, rpcId, grain, typeName, methodName);

				// We can continue on and don't need to disconnect the endpoint as we've
				// handled the method invocation by throwing an exception on the caller.
				reason = null;
				return true;
			}

			SerialTaskScheduler taskScheduler = grain.GetTaskScheduler(methodName);

			Action executeMethod = () =>
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("{0}: Starting RPC #{1}",
						                Name,
						                rpcId);
					}

					try
					{
						TTransport socket = _socket;
						if (socket == null)
						{
							if (Log.IsDebugEnabled)
								Log.DebugFormat("{0}: RPC #{1} interrupted because the socket was disconnected",
								                Name,
								                rpcId);

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
								Log.ErrorFormat("{0}: Caught exception while executing RPC #{1} on {2}.{3} (#{4}): {5}",
								                Name,
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

						EndPointDisconnectReason error;
						if (!SynchronizedWrite(socket, data, responseLength, out error))
						{
							Disconnect(connectionId, error);
						}
					}
					catch (Exception e)
					{
						Log.FatalFormat("{0}: Caught exception while dispatching method invocation, disconnecting: {1}", Name, e);
						Disconnect(connectionId, EndPointDisconnectReason.UnhandledException);
					}
					finally
					{
						if (Log.IsDebugEnabled)
						{
							Log.DebugFormat("{0}: Invocation of RPC #{1} finished",
							                Name,
							                rpcId);
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
				Log.DebugFormat("{0}: Queueing RPC #{1}",
				                Name,
				                rpcId);
			}

			var methodInvocation = new MethodInvocation(rpcId, grain, methodName, task);
			lock (_syncRoot)
				lock (_pendingMethodInvocations)
				{
					if (connectionId != CurrentConnectionId)
					{
						// When this is the case then we're about to queue AND execute
						// a method invocation of a connection that is no longer the current
						// connection, most likely because this endpoint was disconnected
						// between retrieving the message from the socket, and this point.
						// Now that we're disconnected, we can simply ignore this method invocation,
						// NEITHER queueing it NOR executing it.

						if (Log.IsDebugEnabled)
						{
							Log.DebugFormat(
								"{0}: Ignoring RPC invocation request #{1} because it was retrieved from connection '{2}' but now we're in connection '{3}'",
								Name,
								rpcId,
								connectionId,
								CurrentConnectionId);
						}

						reason = null;
						return true;
					}

					MethodInvocation existingMethodInvocation;
					if (_pendingMethodInvocations.TryGetValue(rpcId, out existingMethodInvocation))
					{
						IGrain tmp = existingMethodInvocation.Grain;
						ulong? grainId = tmp?.ObjectId;

						var builder = new StringBuilder();
						builder.AppendFormat("{0}: Received RPC invocation request #{1}, but one with the same id is already pending!",
						                     Name,
						                     rpcId);
						builder.AppendFormat("The original request was made '{0}' on '{1}.{2}",
						                     existingMethodInvocation.RequestTime,
						                     grainId,
						                     existingMethodInvocation.MethodName);
						builder.AppendFormat(" (Total pending requests: {0})", _pendingMethodInvocations.Count);
						Log.Error(builder.ToString());

						reason = EndPointDisconnectReason.RpcDuplicateRequest;
						return false;
					}

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

			reason = null;
			return true;
		}

		private void HandleNoSuchServant(ConnectionId connectionId,
		                                 long rpcId,
		                                 ulong servantId,
		                                 string typeName,
		                                 string methodName,
		                                 int numServants,
		                                 int numProxies)
		{
			TTransport socket = _socket;
			if (socket == null)
			{
				if (Log.IsDebugEnabled)
					Log.DebugFormat("{0}: RPC #{1} interrupted because the socket was disconnected", Name, rpcId);
				return;
			}

			var response = new MemoryStream();
			var writer = new BinaryWriter(response, Encoding.UTF8);
			var exception = new NoSuchServantException(_name, servantId, typeName, methodName, numServants, numProxies);

			response.Position = 0;
			WriteResponseHeader(rpcId, writer, MessageType.Return | MessageType.Exception);
			WriteException(writer, exception);
			PatchResponseMessageLength(response, writer);

			var responseLength = (int) response.Length;
			byte[] data = response.GetBuffer();

			EndPointDisconnectReason error;
			if (!SynchronizedWrite(socket, data, responseLength, out error))
			{
				Disconnect(connectionId, error);
			}
		}

		private void HandleTypeMismatch(ConnectionId connectionId,
		                                long rpcId,
		                                IGrain grain,
		                                string typeName,
		                                string methodName)
		{
			var response = new MemoryStream();
			var writer = new BinaryWriter(response, Encoding.UTF8);
			response.Position = 0;
			WriteResponseHeader(rpcId, writer, MessageType.Return | MessageType.Exception);

			var e = new TypeMismatchException(
				string.Format(
					"{0}: There was a type mismatch when invoking RPC #{1} '{2}' on grain #{3}: Expected '{4}' but found '{5}",
					Name,
					rpcId,
					methodName,
					grain.ObjectId,
					typeName,
					grain.InterfaceType.FullName));

			WriteException(writer, e);
			PatchResponseMessageLength(response, writer);

			var responseLength = (int) response.Length;
			byte[] data = response.GetBuffer();

			EndPointDisconnectReason error;
			TTransport socket = _socket;
			if (!SynchronizedWrite(socket, data, responseLength, out error))
			{
				Disconnect(connectionId, error);
			}
		}

		private bool HandleRequest(ConnectionId connectionId,
		                           long rpcId,
		                           BinaryReader reader,
		                           out EndPointDisconnectReason? disconnectReason)
		{
			ulong servantId = reader.ReadUInt64();
			string typeName = reader.ReadString();
			string methodName = reader.ReadString();
			
			int numServants;
			IServant servant;
			_servants.TryGetServant(servantId, out servant, out numServants);

			if (servant != null)
			{
				return DispatchMethodInvocation(connectionId,
				                                rpcId,
				                                servant,
				                                typeName,
				                                methodName,
				                                reader,
				                                out disconnectReason);
			}

			IProxy proxy;
			int numProxies;
			_proxies.TryGetProxy(servantId, out proxy, out numProxies);

			if (proxy != null)
			{
				return DispatchMethodInvocation(connectionId,
				                                rpcId,
				                                proxy,
				                                typeName,
				                                methodName,
				                                reader,
				                                out disconnectReason);
			}

			//
			// When we couldn't find server nor proxy under the given id, then the user
			// supplied us with a wrong id, already unregistered the servant (or let it be collected
			// by the GC) OR there's a bug somewhere in this project ;)
			// Anyways, we simply serialize the exception so the method call is canceled on the
			// other end and then go on.
			//

			HandleNoSuchServant(connectionId, rpcId, servantId, typeName, methodName, numServants, numProxies);

			disconnectReason = null;
			return true;
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

		private bool TryReadMessage(TTransport socket,
		                            TimeSpan timeout,
		                            string messageStep,
		                            out string messageType,
		                            out string message,
		                            out string error)
		{
			EndPoint remoteEndPoint = GetRemoteEndPointOf(socket);
			var size = new byte[4];
			EndPointDisconnectReason disconnectReason;
			if (!SynchronizedRead(socket, size, timeout, out disconnectReason))
			{
				messageType = null;
				message = null;
				error =
					string.Format(
						"{0}: EndPoint '{1}' did not receive '{2}' message from remote endpoint '{3}' in time: {4}s (error: {5})",
						Name,
						InternalLocalEndPoint,
						messageStep,
						remoteEndPoint,
						timeout.TotalSeconds,
						disconnectReason);
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
			if (!SynchronizedRead(socket, buffer, timeout, out disconnectReason))
			{
				messageType = null;
				message = null;
				error =
					string.Format(
						"{0}: EndPoint '{1}' did not receive '{2}' message from remote endpoint '{3}' in time: {4}s (error: {5})",
						Name,
						InternalLocalEndPoint,
						messageStep,
						remoteEndPoint,
						timeout.TotalSeconds,
						disconnectReason);
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

		/// <summary>
		///     Sends a goodbye message over the socket.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="waitTime"></param>
		/// <returns>True when the goodbye message could be sent, false otherwise</returns>
		private bool SendGoodbye(TTransport socket, TimeSpan waitTime)
		{
			long rpcId = _nextRpcId++;
			return SendGoodbye(socket, rpcId, waitTime);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="waitTime"></param>
		/// <param name="timeSpan"></param>
		/// <returns></returns>
		protected abstract bool SendGoodbye(TTransport socket, long waitTime, TimeSpan timeSpan);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="data"></param>
		/// <param name="offset"></param>
		/// <param name="size"></param>
		protected abstract void Send(TTransport socket, byte[] data, int offset, int size);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="timeout"></param>
		/// <param name="messageStep"></param>
		/// <param name="messageType"></param>
		/// <param name="message"></param>
		/// <exception cref="HandshakeException"></exception>
		protected void ReadMessage(TTransport socket,
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="messageType"></param>
		/// <param name="message"></param>
		/// <exception cref="HandshakeException"></exception>
		protected void WriteMessage(TTransport socket,
		                            string messageType,
		                            string message = "")
		{
			string error;
			if (!TryWriteMessage(socket, messageType, message, out error))
			{
				throw new HandshakeException(error);
			}
		}

		private bool TryWriteMessage(TTransport socket,
		                             string messageType,
		                             string message,
		                             out string error)
		{
			EndPoint remoteEndPoint = GetRemoteEndPointOf(socket);
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				stream.Position = 4;
				writer.Write(messageType);
				writer.Write(message);
				writer.Flush();
				PatchResponseMessageLength(stream, writer);
				stream.Position = 0;

				EndPointDisconnectReason disconnectReason;
				if (!SynchronizedWrite(socket, stream.GetBuffer(), (int) stream.Length, out disconnectReason))
				{
					error = string.Format("{0}: EndPoint '{1}' failed to send {2} to remote endpoint '{3}': {4}",
					                      Name,
					                      InternalLocalEndPoint,
					                      messageType,
					                      remoteEndPoint,
					                      disconnectReason);
					return false;
				}
			}

			error = null;
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <returns></returns>
		protected abstract EndPoint GetRemoteEndPointOf(TTransport socket);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		protected void SendConnectionBlocked(TTransport socket)
		{
			var remoteEndPoint = RemoteEndPoint;
			WriteMessage(socket, EndPointBlocked, remoteEndPoint?.ToString());
		}

		/// <summary>
		///     Performs the authentication between client and server (if necessary) from the server-side.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="remoteEndPoint"></param>
		protected ConnectionId PerformIncomingHandshake(TTransport socket, EndPoint remoteEndPoint)
		{
			TimeSpan timeout = TimeSpan.FromMinutes(1);
			string messageType;
			string message;

			if (_clientAuthenticator != null)
			{
				// Upon accepting an incoming connection, we try to authenticate the client
				// by posing a challenge
				string challenge = _clientAuthenticator.CreateChallenge();
				Log.DebugFormat("{0}: Creating challenge '{1}' for endpoint '{2}'", Name, challenge, remoteEndPoint);
				WriteMessage(socket, AuthenticationRequiredMessage, challenge);

				ReadMessage(socket, timeout, AuthenticationResponse, out messageType, out message);
				Log.DebugFormat("{0}: Received response '{1}' for challenge '{2}' from endpoint '{3}'",
				                Name,
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
				Log.InfoFormat("{0}: Endpoint '{1}' successfully authenticated",
				               Name,
				               remoteEndPoint);
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

			_pendingMethodCalls.IsConnected = true;
			ConnectionId connectionId = OnHandshakeSucceeded(socket, remoteEndPoint);
			WriteMessage(socket, HandshakeSucceedMessage);
			return connectionId;
		}

		/// <summary>
		///     Performs the authentication between client and server (if necessary) from the client-side.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="timeout"></param>
		/// <param name="errorType"></param>
		/// <param name="error"></param>
		/// <param name="currentConnectionId"></param>
		/// <param name="errorReason"></param>
		protected bool TryPerformOutgoingHandshake(TTransport socket,
		                                           TimeSpan timeout,
		                                           out ErrorType errorType,
		                                           out string error,
		                                           out ConnectionId currentConnectionId,
		                                           out object errorReason)
		{
			string messageType;
			string message;
			EndPoint remoteEndPoint = GetRemoteEndPointOf(socket);

			if (!TryReadMessage(socket, timeout, AuthenticationChallenge,
			                    out messageType,
			                    out message,
			                    out error))
			{
				errorType = ErrorType.Handshake;
				currentConnectionId = ConnectionId.None;
				errorReason = null;
				return false;
			}

			if (messageType == AuthenticationRequiredMessage)
			{
				if (_clientAuthenticator == null)
				{
					Log.ErrorFormat("{0}: Server requires client '{1}' to authorize itself, but not authenticator was provided",
					                Name,
					                remoteEndPoint);

					errorType = ErrorType.AuthenticationRequired;
					error = string.Format("Endpoint '{0}' requires authentication", remoteEndPoint);
					currentConnectionId = ConnectionId.None;
					errorReason = null;
					return false;
				}

				string challenge = message;
				Log.DebugFormat("{0}: Server requires client '{1}' to authorize itself, trying to meet challenge '{2}'",
				                Name,
				                remoteEndPoint,
				                challenge);

				// Upon establishing a connection, we try to authenticate the ourselves
				// against the server by answering his response.
				string response = _clientAuthenticator.CreateResponse(challenge);
				if (!TryWriteMessage(socket, AuthenticationResponseMessage, response, out error))
				{
					errorType = ErrorType.Handshake;
					currentConnectionId = ConnectionId.None;
					errorReason = null;
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
					currentConnectionId = ConnectionId.None;
					errorReason = null;
					return false;
				}

				if (messageType != AuthenticationSucceedMessage)
				{
					errorType = ErrorType.Authentication;
					error = string.Format("Failed to authenticate against endpoint '{0}'", remoteEndPoint);
					currentConnectionId = ConnectionId.None;
					errorReason = null;
					return false;
				}
			}
			else if (messageType == NoAuthenticationRequiredMessage)
			{
				Log.Debug("Server requires no client-side authentication");
			}
			else if (messageType == EndPointBlocked)
			{
				errorType = ErrorType.EndPointBlocked;
				errorReason = TryParseEndPoint(message);
				error =
					string.Format("{0}: EndPoint '{1}' is already connected to '{2}' and doesn't accept any other connection until the current one is disconnected",
					              Name,
					              remoteEndPoint,
					              errorReason);
				currentConnectionId = ConnectionId.None;
				return false;
			}
			else
			{
				errorType = ErrorType.Handshake;
				error =
					string.Format("{0}: EndPoint '{1}' sent unknown message '{2}: {3}', expected either {4} or {5}",
					              Name,
					              remoteEndPoint,
					              messageType,
					              message,
					              AuthenticationRequiredMessage,
					              NoAuthenticationRequiredMessage
					             );
				currentConnectionId = ConnectionId.None;
				errorReason = null;
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
					currentConnectionId = ConnectionId.None;
					errorReason = null;
					return false;
				}

				if (!TryReadMessage(socket, timeout, AuthenticationResponse,
				                    out messageType,
				                    out message,
				                    out error))
				{
					errorType = ErrorType.Handshake;
					currentConnectionId = ConnectionId.None;
					errorReason = null;
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
					currentConnectionId = ConnectionId.None;
					errorReason = null;
					return false;
				}

				if (!TryWriteMessage(socket, AuthenticationSucceedMessage, "", out error))
				{
					errorType = ErrorType.Handshake;
					currentConnectionId = ConnectionId.None;
					errorReason = null;
					return false;
				}

				Log.InfoFormat("{0}: Remote endpoint '{1}' successfully authenticated",
				               Name,
				               remoteEndPoint);
			}
			else
			{
				WriteMessage(socket, NoAuthenticationRequiredMessage);
			}

			if (!TryReadMessage(socket, timeout, AuthenticationFinished, out messageType, out message, out error))
			{
				errorType = ErrorType.Handshake;
				currentConnectionId = ConnectionId.None;
				errorReason = null;
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
				currentConnectionId = ConnectionId.None;
				errorReason = null;
				return false;
			}

			_pendingMethodCalls.IsConnected = true;
			currentConnectionId = OnHandshakeSucceeded(socket, remoteEndPoint);
			errorType = ErrorType.Handshake;
			error = null;
			errorReason = null;
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		[Pure]
		protected abstract EndPoint TryParseEndPoint(string message);

		/// <summary>
		///     Is called when the handshake for the newly incoming message succeeds.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="remoteEndPoint"></param>
		protected ConnectionId OnHandshakeSucceeded(TTransport socket, EndPoint remoteEndPoint)
		{
			lock (SyncRoot)
			{
				// There is possibly still a possibility that the _pendingMethodInvocations dictionary
				// contains some entries, EVEN though it's cleared upon being disconnected.
				// For the sake of stability, we'll clear it here again, BEFORE starting
				// the read/write threads, so we most certainly start with a clean slate (once again).
				ClearPendingMethodInvocations();

				Socket = socket;
				InternalRemoteEndPoint = remoteEndPoint;
				CurrentConnectionId = new ConnectionId(Interlocked.Increment(ref _previousConnectionId));
				_cancellationTokenSource = new CancellationTokenSource();

				var args = new ThreadArgs(socket, _cancellationTokenSource.Token, CurrentConnectionId);

				_readThread = new Thread(ReadLoop)
				{
					Name = string.Format("EndPoint '{0}' Socket Reading", Name),
					IsBackground = true,
				};
				_readThread.Start(args);

				_writeThread = new Thread(WriteLoop)
				{
					Name = string.Format("EndPoint '{0}' Socket Writing", Name),
					IsBackground = true,
				};
				_writeThread.Start(args);

				Log.InfoFormat("{0}: Connected to {1}", Name, remoteEndPoint);

				return CurrentConnectionId;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return _name;
		}

		#region Reading from / Writing to socket

		/// <summary>
		///     This method blocks and writes to the given <see cref="ThreadArgs.Socket" /> until
		///     the socket has been disposed of or the <see cref="ThreadArgs.Token" /> has been canceled.
		/// </summary>
		/// <param name="sock"></param>
		protected void WriteLoop(object sock)
		{
			var args = (ThreadArgs) sock;
			TTransport socket = args.Socket;
			CancellationToken token = args.Token;
			ConnectionId currentConnectionId = args.ConnectionId;

			EndPointDisconnectReason disconnectReason;
			try
			{
				while (true)
				{
					if (token.IsCancellationRequested)
					{
						disconnectReason = EndPointDisconnectReason.RequestedByEndPoint;
						break;
					}

					int messageLength;
					byte[] message = _pendingMethodCalls.TakePendingWrite(out messageLength);
					if (message == null)
					{
						disconnectReason = EndPointDisconnectReason.RequestedByEndPoint;
						break;
					}

					if (!SynchronizedWrite(socket, message, messageLength, out disconnectReason))
					{
						break;
					}

					Interlocked.Increment(ref _numMessagesSent);
					Interlocked.Add(ref _numBytesSent, messageLength);
				}
			}
			catch (OperationCanceledException e)
			{
				disconnectReason = EndPointDisconnectReason.RequestedByEndPoint;
				Log.DebugFormat("{0}: Cancelling write loop due to: {1}",
				                Name,
				                e);
			}
			catch (Exception e)
			{
				disconnectReason = EndPointDisconnectReason.UnhandledException;
				Log.ErrorFormat("{0}: Caught exception while writing/handling messages: {1}",
				                Name,
				                e);
			}

			Disconnect(currentConnectionId, disconnectReason);
		}

		/// <summary>
		///     This method blocks and reads from the given <see cref="ThreadArgs.Socket" /> until
		///     the socket has been disposed of or the <see cref="ThreadArgs.Token" /> has been canceled.
		/// </summary>
		/// <param name="sock"></param>
		protected void ReadLoop(object sock)
		{
			var args = (ThreadArgs) sock;
			TTransport socket = args.Socket;
			ConnectionId connectionId = args.ConnectionId;

			EndPointDisconnectReason reason;

			try
			{
				var size = new byte[4];
				while (true)
				{
					if (!SynchronizedRead(socket, size, out reason))
					{
						break;
					}

					int length = BitConverter.ToInt32(size, 0);
					if (length >= 8)
					{
						var buffer = new byte[length];
						if (!SynchronizedRead(socket, buffer, out reason))
						{
							break;
						}

						var stream = new MemoryStream(buffer, false);
						var reader = new BinaryReader(stream);
						long rpcId = reader.ReadInt64();
						var type = (MessageType) reader.ReadByte();

						Interlocked.Add(ref _numBytesReceived, length + 4);
						Interlocked.Increment(ref _numMessagesReceived);
						_lastRead = DateTime.Now;

						EndPointDisconnectReason? r;
						if (!HandleMessage(connectionId, rpcId, type, reader, out r))
						{
							// ReSharper disable PossibleInvalidOperationException
							reason = (EndPointDisconnectReason) r;
							// ReSharper restore PossibleInvalidOperationException

							break;
						}
					}
					else
					{
						reason = EndPointDisconnectReason.ReadFailure;
						break;
					}
				}
			}
			catch (ObjectDisposedException e)
			{
				// We dispose the socket in Disconnect and therefore SynchronizedRead might throw
				// this exception. There should be a better way to lazily dispose of the socket, but
				// the fact that both ReadLoop & WriteLoop access the same socket would require some
				// sort of synchronization that keeps track of both their lifetimes and dispose of the
				// socket once neither thread accesses it anymore.
				reason = EndPointDisconnectReason.RequestedByEndPoint;
				Log.DebugFormat("{0}: Cancelling read loop due to: {1}",
				                Name,
				                e);
			}
			catch (Exception e)
			{
				reason = EndPointDisconnectReason.UnhandledException;
				Log.ErrorFormat("{0}: Caught exception while reading/handling messages: {1}",
				                Name,
				                e);
			}

			Disconnect(connectionId, reason);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="data"></param>
		/// <param name="length"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		protected abstract bool SynchronizedWrite(TTransport socket, byte[] data, int length, out EndPointDisconnectReason error);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="buffer"></param>
		/// <param name="timeout"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		protected abstract bool SynchronizedRead(TTransport socket, byte[] buffer, TimeSpan timeout, out EndPointDisconnectReason error);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="buffer"></param>
		/// <param name="error"></param>
		/// <returns></returns>
		protected abstract bool SynchronizedRead(TTransport socket, byte[] buffer, out EndPointDisconnectReason error);

		#endregion

		/// <summary>
		/// Describes the error which occured because of which the
		/// connection is being dropped.
		/// </summary>
		protected enum ErrorType
		{
			/// <summary>
			/// No error occured.
			/// </summary>
			None,

			/// <summary>
			/// The error occured during the handshake:
			/// This happens when the target isn't a proper SharpRemote endpoint.
			/// </summary>
			Handshake,

			/// <summary>
			/// 
			/// </summary>
			Authentication,

			/// <summary>
			/// 
			/// </summary>
			AuthenticationRequired,
			
			/// <summary>
			/// 
			/// </summary>
			EndPointBlocked
		}

		/// <summary>
		/// The structure given to <see cref="AbstractBinaryStreamEndPoint{TTransport}.ReadLoop"/>
		/// and <see cref="AbstractBinaryStreamEndPoint{TTransport}.WriteLoop"/>.
		/// </summary>
		protected sealed class ThreadArgs
		{
			/// <summary>
			/// 
			/// </summary>
			public readonly ConnectionId ConnectionId;

			/// <summary>
			/// 
			/// </summary>
			public readonly TTransport Socket;

			/// <summary>
			/// 
			/// </summary>
			public readonly CancellationToken Token;

			/// <summary>
			/// 
			/// </summary>
			/// <param name="socket"></param>
			/// <param name="token"></param>
			/// <param name="connectionId"></param>
			public ThreadArgs(TTransport socket, CancellationToken token, ConnectionId connectionId)
			{
				Socket = socket;
				Token = token;
				ConnectionId = connectionId;
			}
		}
	}
}
