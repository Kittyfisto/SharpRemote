using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using log4net;
using SharpRemote.CodeGeneration;
using SharpRemote.Extensions;
using SharpRemote.ServiceDiscovery;
using SharpRemote.Sockets;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This class is responsible for accepting connections from more than one
	///     <see cref="ISocketEndPoint" /> at the same time.
	/// </summary>
	/// <example>
	///     Just like <see cref="ISocketEndPoint" />,
	///     it needs to be bound to a particular address (<see cref="Bind(IPAddress)" />)
	///     or <see cref="IPEndPoint"/> (<see cref="Bind(IPEndPoint)" />).
	/// </example>
	public sealed class SocketServer
		: ISocketServer
	{
		private const int IncomingConnectionBacklog = 10;
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly HashSet<ISocketEndPoint> _connectedEndPoints;
		private readonly HashSet<ISocketEndPoint> _internalEndPoints;

		private readonly string _name;

		private readonly Dictionary<ulong, ISubjectRegistration> _subjects;
		private readonly object _syncRoot;
		private bool _isDisposed;
		private IPEndPoint _localEndPoint;
		private RegisteredService _peerNameRegistration;
		private ISocket _serverSocket;

		/// <summary>
		/// </summary>
		/// <param name="name"></param>
		/// <param name="serverAuthenticator"></param>
		/// <param name="networkServiceDiscoverer"></param>
		/// <param name="clientAuthenticator"></param>
		/// <param name="codeGenerator"></param>
		/// <param name="heartbeatSettings"></param>
		/// <param name="latencySettings"></param>
		/// <param name="endPointSettings"></param>
		public SocketServer(string name = null,
		                    IAuthenticator clientAuthenticator = null,
		                    IAuthenticator serverAuthenticator = null,
		                    INetworkServiceDiscoverer networkServiceDiscoverer = null,
		                    ICodeGenerator codeGenerator = null,
		                    HeartbeatSettings heartbeatSettings = null,
		                    LatencySettings latencySettings = null,
		                    EndPointSettings endPointSettings = null)
		{
			_name = name ?? "<Unnamed>";
			_clientAuthenticator = clientAuthenticator;
			_serverAuthenticator = serverAuthenticator;
			_networkServiceDiscoverer = networkServiceDiscoverer;
			_codeGenerator = codeGenerator ?? CodeGenerator.Default;
			_heartbeatSettings = heartbeatSettings;
			_latencySettings = latencySettings;
			_endPointSettings = endPointSettings;

			_syncRoot = new object();
			_subjects = new Dictionary<ulong, ISubjectRegistration>();
			_internalEndPoints = new HashSet<ISocketEndPoint>();
			_connectedEndPoints = new HashSet<ISocketEndPoint>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (_syncRoot)
			{
				_isDisposed = true;

				if (_connectedEndPoints.Count != 2)
				{
					_isDisposed = true;
				}

				// We copy the collection because it is modified during
				// Disconnect().
				foreach (var endPoint in _connectedEndPoints.ToList())
				{
					TryDisconnectEndPoint(endPoint);
				}
			}

			_serverSocket?.TryDispose();
			_peerNameRegistration?.TryDispose();
		}

		private static void TryDisconnectEndPoint(ISocketEndPoint endPoint)
		{
			try
			{
				endPoint.Disconnect();
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught exception while trying to disconnect endpoint: {0}", e);
			}
		}

		/// <inheritdoc />
		public string Name => _name;

		/// <inheritdoc />
		public IPEndPoint LocalEndPoint => _localEndPoint;

		/// <inheritdoc />
		public void Bind(IPEndPoint ep)
		{
			if (ep == null) throw new ArgumentNullException(nameof(ep));

			var socket = new Socket2(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
			{
				ExclusiveAddressUse = true
			};
			socket.Bind(ep);
			_serverSocket = socket;
			_localEndPoint = ep;
			Listen();
		}

		/// <inheritdoc />
		public void Bind(IPAddress localAddress)
		{
			IPEndPoint ep;
			_serverSocket = Socket2.CreateSocketAndBindToAnyPort(localAddress, out ep);
			_localEndPoint = ep;
			Listen();
		}

		EndPoint IRemotingBase.LocalEndPoint => LocalEndPoint;

		/// <inheritdoc />
		public long NumBytesSent
		{
			get { return Connections.Sum(x => x.NumBytesSent); }
		}

		/// <inheritdoc />
		public long NumBytesReceived
		{
			get { return Connections.Sum(x => x.NumBytesReceived); }
		}

		/// <inheritdoc />
		public long NumMessagesSent
		{
			get { return Connections.Sum(x => x.NumMessagesSent); }
		}

		/// <inheritdoc />
		public long NumMessagesReceived
		{
			get { return Connections.Sum(x => x.NumMessagesReceived); }
		}

		/// <inheritdoc />
		public long NumCallsInvoked
		{
			get { return Connections.Sum(x => x.NumCallsInvoked); }
		}

		/// <inheritdoc />
		public long NumCallsAnswered
		{
			get { return Connections.Sum(x => x.NumCallsAnswered); }
		}

		/// <inheritdoc />
		public long NumPendingMethodCalls
		{
			get { return Connections.Sum(x => x.NumPendingMethodCalls); }
		}

		/// <inheritdoc />
		public long NumPendingMethodInvocations
		{
			get { return Connections.Sum(x => x.NumPendingMethodInvocations); }
		}

		/// <inheritdoc />
		public TimeSpan? AverageRoundTripTime
		{
			get
			{
				return TimeSpan.FromTicks((long) Connections.Average(x =>
				{
					var rtt = x.AverageRoundTripTime;
					if (rtt == null)
						return 0.0;
					return rtt.Value.Ticks;
				}));
			}
		}

		/// <inheritdoc />
		public TimeSpan TotalGarbageCollectionTime
		{
			get { return TimeSpan.FromTicks(Connections.Sum(x => x.TotalGarbageCollectionTime.Ticks)); }
		}

		/// <inheritdoc />
		public EndPointSettings EndPointSettings => _endPointSettings;

		/// <inheritdoc />
		public LatencySettings LatencySettings => _latencySettings;

		/// <inheritdoc />
		public HeartbeatSettings HeartbeatSettings => _heartbeatSettings;

		/// <inheritdoc />
		public void RegisterSubject<T>(ulong objectId, T subject) where T : class
		{
			var registration = new SubjectRegistration<T>(objectId, subject);

			// TODO: This method is missing some sort of verification of the given subject type.
			//       Currently, verification only happens when we are connected to an endpoint.
			//       This is bad because a user of this class only noticies that stuff goes wrong
			//       because no other endpoint can establish connections with this endpoint.

			var stopwatch = Stopwatch.StartNew();

			lock (_syncRoot)
			{
				_subjects.Add(objectId, registration);
				foreach (var endPoint in _internalEndPoints)
					registration.RegisterSubjectWith(endPoint);
			}

			stopwatch.Stop();
			Log.DebugFormat("{0}: Created new servant (#{1}) '{2}' implementing '{3}', took {4}ms",
			                _name,
			                objectId,
			                subject,
			                typeof(T),
			                stopwatch.ElapsedMilliseconds);
		}

		/// <inheritdoc />
		public IEnumerable<IRemotingEndPoint> Connections
		{
			get
			{
				lock (_syncRoot)
				{
					return _connectedEndPoints.ToList();
				}
			}
		}

		/// <inheritdoc />
		public event Action<IRemotingEndPoint> OnClientConnected;

		/// <inheritdoc />
		public event Action<IRemotingEndPoint> OnClientDisconnected;

		/// <inheritdoc />
		public override string ToString()
		{
			return _name;
		}

		private void Listen()
		{
			_serverSocket.Listen(IncomingConnectionBacklog);
			Log.InfoFormat("{0}: EndPoint listening on {1}", Name, LocalEndPoint);

			if (Name != null && _networkServiceDiscoverer != null)
			{
				_peerNameRegistration = _networkServiceDiscoverer.RegisterService(Name, LocalEndPoint);
				Log.InfoFormat("{0}: Endpoint '{1}' published to local cloud", Name, LocalEndPoint);
			}

			BeginAccept();
		}

		private void BeginAccept()
		{
			lock (_syncRoot)
			{
				if (!_isDisposed)
					_serverSocket.BeginAccept(OnIncomingConnection, state: null);
			}
		}

		private void OnIncomingConnection(IAsyncResult ar)
		{
			if (_isDisposed)
				return;

			SocketEndPoint endPoint = null;
			try
			{
				endPoint = CreateAndAddEndpoint();
				endPoint.OnIncomingConnection(_serverSocket, ar);

				lock (_syncRoot)
				{
					// We need to check here again because if we've been
					// disposed of in the meantime, then we should close
					// this incoming connection again and shut down...
					if (_isDisposed)
					{
						DisposeAndRemoveEndPoint(endPoint);
						return;
					}

					_connectedEndPoints.Add(endPoint);
				}

				// This should never be called from within a lock!
				EmitOnClientConnected(endPoint);

				// DO NOT ADD ANY MORE CODE AFTER THIS LINE
			}
			catch (Exception e)
			{
				Log.ErrorFormat("{0}: Caught exception while accepting incoming connection: {1}",
				                Name,
				                e);

				DisposeAndRemoveEndPoint(endPoint);
			}
			finally
			{
				if (!_isDisposed)
				{
					BeginAccept();
				}
				else
				{
					Log.DebugFormat("{0}: No longer calling BeginAccept(): We've been disposed of",
					                Name);
				}
			}
		}

		private void DisposeAndRemoveEndPoint(SocketEndPoint endPoint)
		{
			endPoint?.TryDispose();

			lock (_syncRoot)
			{
				_internalEndPoints.Remove(endPoint);
				_connectedEndPoints.Remove(endPoint);
			}
		}

		/// <summary>
		///     Creates a new endpoint, registers all currently known subjects
		///     with it and adds it to the <see cref="_internalEndPoints" /> list
		///     in one atomic operation.
		/// </summary>
		/// <returns></returns>
		private SocketEndPoint CreateAndAddEndpoint()
		{
			var endPoint = new SocketEndPoint(EndPointType.Server,
			                                  _name,
			                                  _clientAuthenticator,
			                                  _serverAuthenticator,
			                                  networkServiceDiscoverer: null,
			                                  codeGenerator: _codeGenerator,
			                                  heartbeatSettings: _heartbeatSettings,
			                                  latencySettings: _latencySettings,
			                                  endPointSettings: _endPointSettings);
			try
			{
				var stopwatch = Stopwatch.StartNew();

				lock (_syncRoot)
				{
					foreach (var subjectRegistration in _subjects.Values)
						subjectRegistration.RegisterSubjectWith(endPoint);

					endPoint.OnDisconnected += (ep, connectionId) => EndPointOnOnDisconnected(endPoint, connectionId);
					_internalEndPoints.Add(endPoint);

					// DO NOT ADD ANYTHING ELSE AFTER HERE
				}

				stopwatch.Stop();
				Log.DebugFormat("{0}: Registering subjects with newly created endpoint took {1}ms",
				                _name,
				                stopwatch.ElapsedMilliseconds);
			}
			catch (Exception)
			{
				endPoint.Dispose();
				_internalEndPoints.Remove(endPoint);
				throw;
			}

			return endPoint;
		}

		private void EndPointOnOnDisconnected(SocketEndPoint endPoint, ConnectionId connectionId)
		{
			DisposeAndRemoveEndPoint(endPoint);

			// This should never be called from within a lock!
			EmitOnClientDisconnected(endPoint);
		}

		private void EmitOnClientConnected(IRemotingEndPoint endPoint)
		{
			OnClientConnected?.Invoke(endPoint);
		}

		private void EmitOnClientDisconnected(IRemotingEndPoint endPoint)
		{
			OnClientDisconnected?.Invoke(endPoint);
		}

		/// <summary>
		///     Required in order to capture the type parameter of
		///     <see cref="SocketServer.RegisterSubject{T}" />.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		private sealed class SubjectRegistration<T>
			: ISubjectRegistration
			where T : class
		{
			private readonly ulong _objectId;
			private readonly T _subject;

			public SubjectRegistration(ulong objectId, T subject)
			{
				_objectId = objectId;
				_subject = subject;
			}

			public void RegisterSubjectWith(ISocketEndPoint endPoint)
			{
				endPoint.CreateServant(_objectId, _subject);
			}
		}

		internal interface ISubjectRegistration
		{
			void RegisterSubjectWith(ISocketEndPoint endPoint);
		}

		#region EndPoint configuration

		private readonly IAuthenticator _clientAuthenticator;
		private readonly IAuthenticator _serverAuthenticator;
		private readonly INetworkServiceDiscoverer _networkServiceDiscoverer;
		private readonly ICodeGenerator _codeGenerator;
		private readonly HeartbeatSettings _heartbeatSettings;
		private readonly LatencySettings _latencySettings;
		private readonly EndPointSettings _endPointSettings;

		#endregion
	}
}