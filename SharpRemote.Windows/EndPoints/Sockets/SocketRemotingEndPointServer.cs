using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using SharpRemote.ServiceDiscovery;
using log4net;
using SharpRemote.CodeGeneration;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// A socket remoting end point that accepts one incoming connection from a <see cref="SocketRemotingEndPointClient"/>.
	/// </summary>
	public sealed class SocketRemotingEndPointServer
		: AbstractIPSocketRemotingEndPoint
	{
		private static new readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly INetworkServiceDiscoverer _networkServiceDiscoverer;

		private RegisteredService _peerNameRegistration;
		private Socket _serverSocket;
		private bool _isConnecting;

		/// <summary>
		///     Creates a new socket end point that (optionally) is bound to the given
		///     P2P name, otherwise the name is only used for debugging.
		/// </summary>
		/// <param name="name">The name of this socket, used to publish it via PNRP as well as to refer to this endpoint in diagnostic output</param>
		/// <param name="clientAuthenticator">The authenticator, if any, to authenticate a client against a server (both need to use the same authenticator)</param>
		/// <param name="serverAuthenticator">The authenticator, if any, to authenticate a server against a client (both need to use the same authenticator)</param>
		/// <param name="networkServiceDiscoverer">The discoverer used to register this server as a service with the name <paramref name="name"/> and whatever endpoint <see cref="Bind(IPAddress)"/> is given</param>
		/// <param name="codeGenerator">The code generator to create proxy and servant types</param>
		/// <param name="heartbeatSettings">The settings for heartbeat mechanism, if none are specified, then default settings are used</param>
		/// <param name="latencySettings">The settings for latency measurements, if none are specified, then default settings are used</param>
		/// <param name="endPointSettings">The settings for the endpoint itself (max. number of concurrent calls, etc...)</param>
		public SocketRemotingEndPointServer(string name = null,
		                                    IAuthenticator clientAuthenticator = null,
		                                    IAuthenticator serverAuthenticator = null,
		                                    INetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                    ICodeGenerator codeGenerator = null,
		                                    HeartbeatSettings heartbeatSettings = null,
		                                    LatencySettings latencySettings = null,
		                                    EndPointSettings endPointSettings = null)
			: base(EndPointType.Server,
			       name,
			       clientAuthenticator,
			       serverAuthenticator,
			       codeGenerator,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{
			_networkServiceDiscoverer = networkServiceDiscoverer;
		}

		/// <summary>
		///     Binds this socket
		/// </summary>
		/// <param name="ep"></param>
		public void Bind(IPEndPoint ep)
		{
			if (ep == null) throw new ArgumentNullException(nameof(ep));

			var socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
				{
					ExclusiveAddressUse = true
				};
			socket.Bind(ep);
			_serverSocket = socket;
			LocalEndPoint = ep;
			Listen();
		}

		/// <summary>
		///     Binds this socket
		/// </summary>
		/// <param name="localAddress"></param>
		public void Bind(IPAddress localAddress)
		{
			if (localAddress == null) throw new ArgumentNullException(nameof(localAddress));
			if (IsConnected)
				throw new InvalidOperationException("A socket may only bound to a particular port when its not already connected");

			IPEndPoint ep;
			_serverSocket = CreateSocketAndBindToAnyPort(localAddress, out ep);
			LocalEndPoint = ep;
			Listen();
		}

		private void Listen()
		{
			_serverSocket.Listen(1);
			_serverSocket.BeginAccept(OnIncomingConnection, null);
			Log.InfoFormat("{0}: EndPoint listening on {1}", Name, LocalEndPoint);

			if (Name != null && _networkServiceDiscoverer != null)
			{
				_peerNameRegistration = _networkServiceDiscoverer.RegisterService(Name, LocalEndPoint);
				Log.InfoFormat("{0}: Endpoint '{1}' published to local cloud", Name, LocalEndPoint);
			}
		}

		/// <inheritdoc />
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
			}

			Socket socket = null;
			bool success = false;
			try
			{
				socket = _serverSocket.EndAccept(ar);

				bool isAlreadyConnected;
				lock (SyncRoot)
				{
					isAlreadyConnected = InternalRemoteEndPoint != null ||
					                     _isConnecting;
					_isConnecting = true;
				}

				if (isAlreadyConnected)
				{
					Log.InfoFormat("{0}: Blocking incoming connection from '{1}', we're already connected to another endpoint",
					               Name,
					               socket.RemoteEndPoint);
				}
				else
				{
					Log.DebugFormat("{0}: Incoming connection from '{1}', starting handshake...",
					                Name,
					                socket.RemoteEndPoint);

					var connectionId = PerformIncomingHandshake(socket, socket.RemoteEndPoint);
					FireOnConnected(socket.RemoteEndPoint, connectionId);

					success = true;
				}
			}
			catch (AuthenticationException e)
			{
				Log.WarnFormat("{0}: Closing connection: {1}",
				               Name,
				               e);

				Disconnect();
			}
			catch (Exception e)
			{
				Log.ErrorFormat("{0}: Caught exception while accepting incoming connection - disconnecting again: {1}",
				                Name,
				                e);

				Disconnect();
			}
			finally
			{
				if (!success)
				{
					if (socket != null)
					{
						try
						{
							socket.Shutdown(SocketShutdown.Both);
							socket.Disconnect(false);
							socket.Dispose();
						}
						catch (Exception e)
						{
							Log.WarnFormat("{0}: Ignoring exception caught while disconnecting & disposing of socket: {1}",
							               Name,
							               e);
						}
					}
				}

				lock (SyncRoot)
				{
					_isConnecting = false;
					if (!IsDisposed)
					{
						_serverSocket.BeginAccept(OnIncomingConnection, null);
					}
				}
			}
		}

		internal static Socket CreateSocketAndBindToAnyPort(IPAddress address, out IPEndPoint localAddress)
		{
			const ushort firstSocket = 49152;
			const ushort lastSocket = 65535;

			return CreateSocketAndBindToAnyPort(address, firstSocket, lastSocket, out localAddress);
		}

		internal static Socket CreateSocketAndBindToAnyPort(IPAddress address,
			ushort firstSocket,
			ushort lastSocket,
			out IPEndPoint localAddress)
		{
			AddressFamily family = address.AddressFamily;
			var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				socket.ExclusiveAddressUse = true;

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
	}
}
