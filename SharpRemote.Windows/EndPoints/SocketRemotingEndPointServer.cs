using System;
using System.Net;
using System.Net.PeerToPeer;
using System.Net.Sockets;
using System.Reflection;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class SocketRemotingEndPointServer
		: AbstractIPSocketRemotingEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private PeerNameRegistration _peerNameRegistration;
		private Socket _serverSocket;

		/// <summary>
		///     Creates a new socket end point that (optionally) is bound to the given
		///     P2P name, if PNRP is available, otherwise the name is only used for debugging.
		/// </summary>
		/// <remarks>
		///     Currently, no exception is thrown when the required P2P service "PNRPsvc" is
		///     not installed or not running. Check the <see cref="AbstractIPSocketRemotingEndPoint.IsP2PAvailable" /> flag to
		///     find out if it is.
		/// </remarks>
		/// <param name="name">The name of this socket, used to publish it via PNRP as well as to refer to this endpoint in diagnostic output</param>
		/// <param name="clientAuthenticator">The authenticator, if any, to authenticate a client against a server (both need to use the same authenticator)</param>
		/// <param name="serverAuthenticator">The authenticator, if any, to authenticate a server against a client (both need to use the same authenticator)</param>
		/// <param name="customTypeResolver">The type resolver, if any, responsible for resolving Type objects by their assembly qualified name</param>
		public SocketRemotingEndPointServer(string name = null,
		                              IAuthenticator clientAuthenticator = null,
		                              IAuthenticator serverAuthenticator = null,
		                              ITypeResolver customTypeResolver = null)
			: base(EndPointType.Server,
			name,
			clientAuthenticator,
			serverAuthenticator,
			customTypeResolver)
		{
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

			_serverSocket.Listen(1);
			_serverSocket.BeginAccept(OnIncomingConnection, null);
			Log.InfoFormat("EndPoint '{0}' listening on {1}", Name, LocalEndPoint);

			if (Name != null && IsP2PAvailable)
			{
				var peerName = new PeerName(Name, PeerNameType.Unsecured);
				_peerNameRegistration = new PeerNameRegistration
					{
						PeerName = peerName,
						Port = LocalEndPoint.Port,
					};
				_peerNameRegistration.Start();
				Log.InfoFormat("Endpoint '{0}@{1}' published to local cloud via PNRP", Name, LocalEndPoint);
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

				if (Log.IsDebugEnabled)
				{
					Log.DebugFormat("Incoming connection from '{0}', starting handshake...",
					                socket.RemoteEndPoint);
				}

				PerformIncomingHandshake(socket);

				success = true;
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
				if (!success && socket != null)
				{
					socket.Shutdown(SocketShutdown.Both);
					socket.Disconnect(false);
					socket.Dispose();
				}

				lock (SyncRoot)
				{
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
