using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using SharpRemote.ServiceDiscovery;
using log4net;

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
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly NetworkServiceDiscoverer _networkServiceDiscoverer;

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
		/// <param name="customTypeResolver">The type resolver, if any, responsible for resolving Type objects by their assembly qualified name</param>
		/// <param name="networkServiceDiscoverer">The discoverer used to register this server as a service with the name <paramref name="name"/> and whatever endpoint <see cref="Bind(IPAddress)"/> is given</param>
		/// <param name="serializer">The serializer used to serialize and deserialize values - if none is specified then a new one is created</param>
		public SocketRemotingEndPointServer(string name = null,
		                              IAuthenticator clientAuthenticator = null,
		                              IAuthenticator serverAuthenticator = null,
		                                    ITypeResolver customTypeResolver = null,
		                                    NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                    Serializer serializer = null)
			: base(EndPointType.Server,
			       name,
			       clientAuthenticator,
			       serverAuthenticator,
			       customTypeResolver,
			       serializer)
		{
			_networkServiceDiscoverer = networkServiceDiscoverer;
		}

		/// <summary>
		///     Binds this socket
		/// </summary>
		/// <param name="ep"></param>
		public void Bind(IPEndPoint ep)
		{
			if (ep == null) throw new ArgumentNullException("ep");

			var socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
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
			if (localAddress == null) throw new ArgumentNullException("localAddress");
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
			Log.InfoFormat("EndPoint '{0}' listening on {1}", Name, LocalEndPoint);

			if (Name != null && _networkServiceDiscoverer != null)
			{
				_peerNameRegistration = _networkServiceDiscoverer.RegisterService(Name, LocalEndPoint);
				Log.InfoFormat("Endpoint '{0}@{1}' published to local cloud", Name, LocalEndPoint);
			}
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
					Log.InfoFormat("Blocking incoming connection from '{0}', we're already connected to another endpoint",
					               socket.RemoteEndPoint);
				}
				else
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("Incoming connection from '{0}', starting handshake...",
										socket.RemoteEndPoint);
					}

					PerformIncomingHandshake(socket);

					FireOnConnected(socket.RemoteEndPoint);

					success = true;
				}
			}
			catch (AuthenticationException e)
			{
				Log.WarnFormat("Closing connection: {0}", e);

				Disconnect();
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught exception while accepting incoming connection - disconnecting again: {0}", e);

				Disconnect();
			}
			finally
			{
				if (!success)
				{
					if (socket != null)
					{
						socket.Shutdown(SocketShutdown.Both);
						socket.Disconnect(false);
						socket.Dispose();
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
	}
}
