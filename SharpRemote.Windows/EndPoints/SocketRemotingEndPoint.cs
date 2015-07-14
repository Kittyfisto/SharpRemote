using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.PeerToPeer;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using log4net;

// ReSharper disable CheckNamespace

namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     <see cref="IRemotingEndPoint" /> implementation that establishes a TCP socket with another
	///     endPoint. A listening socket is opened (and bound to an address) with <see cref="Bind" /> while
	///     a connectiong to such a socket is established with <see cref="Connect(IPEndPoint)" /> or
	///     <see cref="Connect(string)" />.
	/// </summary>
	public sealed class SocketRemotingEndPoint
		: AbstractSocketRemotingEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private CancellationTokenSource _cancellationTokenSource;
		private IPEndPoint _localEndPoint;
		private PeerNameRegistration _peerNameRegistration;
		private Task _readTask;
		private IPEndPoint _remoteEndPoint;
		private Socket _serverSocket;

		/// <summary>
		///     Creates a new socket end point that (optionally) is bound to the given
		///     P2P name, if PNRP is available, otherwise the name is only used for debugging.
		/// </summary>
		/// <remarks>
		///     Currently, no exception is thrown when the required P2P service "PNRPsvc" is
		///     not installed or not running. Check the <see cref="IsP2PAvailable" /> flag to
		///     find out if it is.
		/// </remarks>
		/// <param name="name">The name of this socket, used to publish it via PNRP as well as to refer to this endpoint in diagnostic output</param>
		/// <param name="clientAuthenticator">The authenticator, if any, to authenticate a client against a server (both need to use the same authenticator)</param>
		/// <param name="serverAuthenticator">The authenticator, if any, to authenticate a server against a client (both need to use the same authenticator)</param>
		public SocketRemotingEndPoint(string name = null,
		                              IAuthenticator clientAuthenticator = null,
		                              IAuthenticator serverAuthenticator = null)
			: base(name,
			clientAuthenticator,
			serverAuthenticator)
		{
		}

		/// <summary>
		///     Whether or not the P2P name publishing service is available on this machine or not.
		///     Is required to <see cref="Bind" /> a socket to a particular name (as well as a particular port)
		///     and to <see cref="Connect(string)" /> to that socket.
		/// </summary>
		public static bool IsP2PAvailable
		{
			get
			{
				ServiceController sc = ServiceController.GetServices().FirstOrDefault(x => x.ServiceName == "PNRPsvc");

				if (sc == null)
					return false;

				if (sc.Status == ServiceControllerStatus.Running)
					return true;

				return false;
			}
		}

		/// <summary>
		///     IPAddress+Port pair of this endPoint in case <see cref="Bind" /> has been called.
		///     Otherwise null.
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get { return _localEndPoint; }
		}

		/// <summary>
		///     IPAddress+Port pair of the connected endPoint in case <see cref="Connect(IPEndPoint)" /> has been called.
		///     Otherwise null.
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

		/// <summary>
		///     Binds this socket
		/// </summary>
		/// <param name="localAddress"></param>
		public void Bind(IPAddress localAddress)
		{
			if (localAddress == null) throw new ArgumentNullException("localAddress");
			if (IsConnected)
				throw new InvalidOperationException("A socket may only bound to a particular port when its not already connected");

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
		///     Connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		public void Connect(string endPointName)
		{
			Connect(endPointName, TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentException">
		///     In case <paramref name="endPointName" /> is null
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">When no such endPoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="SocketRemotingEndPoint" />
		///     - The given endPoint failed authentication
		/// </exception>
		public void Connect(string endPointName, TimeSpan timeout)
		{
			if (endPointName == null) throw new ArgumentNullException("endPointName");

			var resolver = new PeerNameResolver();
			PeerNameRecordCollection results = resolver.Resolve(new PeerName(endPointName, PeerNameType.Unsecured));

			if (results.Count == 0)
			{
				Log.ErrorFormat("Unable to find peer named '{0}'", endPointName);
				throw new NoSuchIPEndPointException(endPointName);
			}

			PeerNameRecord peer = results[0];
			IPEndPointCollection endPoints = peer.EndPointCollection;

			foreach (IPEndPoint ep in endPoints)
			{
				try
				{
					Connect(ep, timeout);
					break;
				}
				catch (NoSuchIPEndPointException) //< Let's try the next...
				{
				}
			}
		}

		/// <summary>
		///     Connects this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// ///
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">When no such endPoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="SocketRemotingEndPoint" />
		///     - The given endPoint failed authentication
		/// </exception>
		public void Connect(IPEndPoint endPoint)
		{
			Connect(endPoint, TimeSpan.FromSeconds(1));
		}

		/// <summary>
		///     Connects this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">The amount of time this method should block and await a successful connection from the remote end-point</param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">When no such endPoint could be *found* - it might exist but this one is incapable of establishing a successfuly connection</exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="SocketRemotingEndPoint" />
		///     - The given endPoint failed authentication
		/// </exception>
		public void Connect(IPEndPoint endPoint, TimeSpan timeout)
		{
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (Equals(endPoint, _localEndPoint))
				throw new ArgumentException("An endPoint cannot be connected to itself", "endPoint");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeout");
			if (IsConnected)
				throw new InvalidOperationException(
					"This endPoint is already connected to another endPoint and cannot establish any more connections");

			Log.DebugFormat("Trying to connect to '{0}', timeout: {1}ms", endPoint, timeout.TotalMilliseconds);

			bool success = false;
			Socket socket = null;
			try
			{
				DateTime started = DateTime.Now;
				var task = new Task(() =>
					{
						socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
						socket.Connect(endPoint);
					});
				task.Start();
				if (!task.Wait(timeout))
					throw new NoSuchIPEndPointException(endPoint);

				TimeSpan remaining = timeout - (DateTime.Now - started);
				PerformOutgoingHandshake(socket, remaining);
				_remoteEndPoint = endPoint;

				success = true;
			}
			catch (AggregateException e)
			{
				ReadOnlyCollection<Exception> inner = e.InnerExceptions;
				if (inner.Count != 1)
					throw;

				Exception ex = inner[0];
				if (!(ex is SocketException))
					throw;

				throw new NoSuchIPEndPointException(endPoint, e);
			}
			catch (SocketException e)
			{
				throw new NoSuchIPEndPointException(endPoint, e);
			}
			finally
			{
				if (!success)
				{
					if (socket != null)
					{
						socket.Close();
						socket.Dispose();
					}

					_remoteEndPoint = null;
				}
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

				_serverSocket.BeginAccept(OnIncomingConnection, null);
			}
		}

		protected override void OnHandshakeSucceeded(Socket socket)
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
	}
}