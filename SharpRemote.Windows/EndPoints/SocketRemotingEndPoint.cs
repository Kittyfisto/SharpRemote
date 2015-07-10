using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.PeerToPeer;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public sealed class SocketRemotingEndPoint
		: AbstractSocketRemotingEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private PeerNameRegistration _peerNameRegistration;

		private CancellationTokenSource _cancellationTokenSource;
		private IPEndPoint _localEndPoint;
		private IPEndPoint _remoteEndPoint;
		private Socket _serverSocket;
		private Task _readTask;

		public static bool IsP2PAvailable
		{
			get
			{
				var sc = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == "PNRPsvc");

				if (sc == null)
					return false;

				if (sc.Status == ServiceControllerStatus.Running)
					return true;

				return false;
			}
		}

		public SocketRemotingEndPoint(string name = null)
			: base(name)
		{}

		/// <summary>
		/// IPAddress+Port pair of this endpoint in case <see cref="Bind"/> has been called.
		/// Otherwise null.
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get { return _localEndPoint; }
		}

		/// <summary>
		/// IPAddress+Port pair of the connected endpoint in case <see cref="Connect"/> has been called.
		/// Otherwise null.
		/// </summary>
		public IPEndPoint RemoteEndPoint
		{
			get { return _remoteEndPoint; }
		}

		protected override EndPoint InternalLocalEndPoint
		{
			get { return _localEndPoint; }
		}

		protected override EndPoint InternalRemoteEndPoint
		{
			get { return _remoteEndPoint; }
			set { _remoteEndPoint = (IPEndPoint) value; }
		}

		protected override void DisposeAdditional()
		{
			_serverSocket.TryDispose();
			if (_peerNameRegistration != null)
			{
				_peerNameRegistration.TryDispose();
			}
		}

		private void OnIncomingConnection(IAsyncResult ar)
		{
			lock (SyncRoot)
			{
				if (IsDisposed)
					return;

				try
				{
					OnConnected(_serverSocket.EndAccept(ar));
					SendWelcomeMessage();
				}
				catch (Exception e)
				{
					Log.ErrorFormat("Caught exception while accepting incoming connection - disconnecting again: {0}", e);
					Disconnect();
				}
				finally
				{
					_serverSocket.BeginAccept(OnIncomingConnection, null);
				}
			}
		}

		private void OnConnected(Socket socket)
		{
			lock (SyncRoot)
			{
				Socket = socket;
				_remoteEndPoint = (IPEndPoint) socket.RemoteEndPoint;
				_cancellationTokenSource = new CancellationTokenSource();
				_readTask = new Task(Read, new KeyValuePair<Socket, CancellationToken>(Socket, _cancellationTokenSource.Token));
				_readTask.Start();

				Log.InfoFormat("{0}: Connected to {1}", Name, _remoteEndPoint);
			}
		}

		private Socket CreateSocketAndBindToAnyPort(IPAddress address, out IPEndPoint localAddress)
		{
			AddressFamily family = address.AddressFamily;
			var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				const ushort firstSocket = 49152;
				const ushort lastSocket = 65535;

				localAddress = null;
				for (ushort i = firstSocket; i <= lastSocket; ++i)
				{
					try
					{
						localAddress = new IPEndPoint(address, i);
						socket.Bind(localAddress);
						break;
					}
					catch (SocketException)
					{
					}
				}

				if (!socket.IsBound)
					throw new SystemException("No more available sockets");

				return socket;
			}
			finally
			{
				if (!socket.IsBound)
					socket.Dispose();
			}
		}

		public void Bind(IPAddress localAddress)
		{
			if (localAddress == null) throw new ArgumentNullException("localAddress");

			_serverSocket = CreateSocketAndBindToAnyPort(localAddress, out _localEndPoint);
			_serverSocket.Listen(1);
			_serverSocket.BeginAccept(OnIncomingConnection, null);
			Log.InfoFormat("EndPoint '{0}' listening on {1}", Name, _localEndPoint);

			if (Name != null && IsP2PAvailable)
			{
				var peerName = new PeerName(Name, PeerNameType.Unsecured);
				_peerNameRegistration = new PeerNameRegistration
				{
					PeerName = peerName,
					Port = _localEndPoint.Port,
				};
				_peerNameRegistration.Start();
				Log.InfoFormat("Endpoint '{0}@{1}' published to local cloud via PNRP", Name, _localEndPoint);
			}
		}

		/// <summary>
		/// Connects to another endpoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentException">In case <paramref name="endPointName"/> is null</exception>
		public void Connect(string endPointName, TimeSpan timeout)
		{
			if (endPointName == null) throw new ArgumentNullException("endPointName");

			var resolver = new PeerNameResolver();
			var results = resolver.Resolve(new PeerName(endPointName, PeerNameType.Unsecured));

			if (results.Count == 0)
			{
				Log.ErrorFormat("Unable to find peer named '{0}'", endPointName);
				throw new NoSuchEndPointException(endPointName);
			}

			var peer = results[0];
			var endPoints = peer.EndPointCollection;

			foreach (var ep in endPoints)
			{
				try
				{
					Connect(ep, timeout);
					break;
				}
				catch (NoSuchEndPointException) //< Let's try the next...
				{}
			}
		}

		public void Connect(IPEndPoint endPoint)
		{
			Connect(endPoint, TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Connects this endpoint to the given one.
		/// </summary>
		/// <param name="endpoint"></param>
		/// <param name="timeout">The amount of time this method should block and await a successful connection from the remote end-point</param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endpoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endpoint is already connected to another endpoint.
		/// </exception>
		/// <exception cref="NoSuchEndPointException">When no such endpoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		/// <exception cref="InvalidEndPointException">The given endpoint is no <see cref="SocketRemotingEndPoint"/></exception>
		public void Connect(IPEndPoint endpoint, TimeSpan timeout)
		{
			if (endpoint == null) throw new ArgumentNullException("endpoint");
			if (Equals(endpoint, _localEndPoint))
				throw new ArgumentException("An endpoint cannot be connected to itself", "endpoint");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeout");
			if (IsConnected)
				throw new InvalidOperationException(
					"This endpoint is already connected to another endpoint and cannot establish any more connections");

			Log.DebugFormat("Trying to connect to '{0}', timeout: {1}ms", endpoint, timeout.TotalMilliseconds);

			Socket socket = null;
			try
			{
				DateTime started = DateTime.Now;
				var task = new Task(() =>
					{
						socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
						socket.Connect(endpoint);
					});
				task.Start();
				if (!task.Wait(timeout))
					throw new NoSuchEndPointException(endpoint);

				TimeSpan remaining = timeout - (DateTime.Now - started);
				if (!ReadWelcomeMessage(socket, remaining, endpoint))
					throw new InvalidEndPointException(endpoint);

				_remoteEndPoint = endpoint;
				OnConnected(socket);
			}
			catch (AggregateException e)
			{
				var inner = e.InnerExceptions;
				if (inner.Count != 1)
					throw;

				var ex = inner[0];
				if (!(ex is SocketException))
					throw;

				throw new NoSuchEndPointException(endpoint, e);
			}
			catch (SocketException e)
			{
				throw new NoSuchEndPointException(endpoint, e);
			}
			catch (Exception)
			{
				if (socket != null)
					socket.Dispose();
				throw;
			}
		}
	}
}