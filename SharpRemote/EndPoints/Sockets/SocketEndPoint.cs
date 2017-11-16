using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using SharpRemote.CodeGeneration;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using SharpRemote.ServiceDiscovery;
using SharpRemote.Sockets;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     An endpoint which can establish a connection with exactly one other <see cref="SocketEndPoint" />.
	///     One endpoints needs to <see cref="Bind(System.Net.IPAddress)" /> itself to an address/endpoint while
	///     the other needs to <see cref="Connect(string)" /> to it (just like <see cref="Socket" />).
	/// </summary>
	/// <remarks>
	///     A <see cref="EndPointType.Server" /> can only accept one concurrent connection at the moment.
	///     See issue #36 on github for more information on how a true server endpoint will be implemented.
	/// </remarks>
	public sealed class SocketEndPoint
		: AbstractBinaryStreamEndPoint<ISocket>
			, ISocketEndPoint
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly INetworkServiceDiscoverer _networkServiceDiscoverer;
		private bool _isConnecting;
		private RegisteredService _peerNameRegistration;

		private ISocket _serverSocket;

		/// <summary>
		///     Creates a new socket end point that (optionally) is bound to the given
		///     P2P name, if PNRP is available, otherwise the name is only used for debugging.
		/// </summary>
		/// <param name="type">The type of endpoint to create</param>
		/// <param name="name">
		///     The name of this socket, used to publish it via PNRP as well as to refer to this endpoint in
		///     diagnostic output
		/// </param>
		/// <param name="clientAuthenticator">
		///     The authenticator, if any, to authenticate a client against a server (both need to
		///     use the same authenticator)
		/// </param>
		/// <param name="serverAuthenticator">
		///     The authenticator, if any, to authenticate a server against a client (both need to
		///     use the same authenticator)
		/// </param>
		/// <param name="networkServiceDiscoverer">The discoverer used to find services by name within the local network</param>
		/// <param name="codeGenerator">The code generator to create proxy and servant types</param>
		/// <param name="heartbeatSettings">
		///     The settings for heartbeat mechanism, if none are specified, then default settings are
		///     used
		/// </param>
		/// <param name="latencySettings">
		///     The settings for latency measurements, if none are specified, then default settings are
		///     used
		/// </param>
		/// <param name="endPointSettings">The settings for the endpoint itself (max. number of concurrent calls, etc...)</param>
		public SocketEndPoint(EndPointType type,
		                      string name = null,
		                      IAuthenticator clientAuthenticator = null,
		                      IAuthenticator serverAuthenticator = null,
		                      INetworkServiceDiscoverer networkServiceDiscoverer = null,
		                      ICodeGenerator codeGenerator = null,
		                      HeartbeatSettings heartbeatSettings = null,
		                      LatencySettings latencySettings = null,
		                      EndPointSettings endPointSettings = null)
			: base(new GrainIdGenerator(type),
			       name,
			       type,
			       clientAuthenticator,
			       serverAuthenticator,
			       codeGenerator,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{
			_networkServiceDiscoverer = networkServiceDiscoverer;
		}

		/// <inheritdoc />
		protected override EndPoint InternalRemoteEndPoint
		{
			get { return RemoteEndPoint; }
			set { RemoteEndPoint = (IPEndPoint) value; }
		}

		/// <inheritdoc />
		protected override EndPoint InternalLocalEndPoint => LocalEndPoint;

		/// <summary>
		///     Tries to connect to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <returns>True when a connection could be established, false otherwise</returns>
		public bool TryConnect(string endPointName)
		{
			return TryConnect(endPointName, TimeSpan.FromSeconds(value: 1));
		}

		/// <summary>
		///     Tries to connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <returns>True when the connection succeeded, false otherwise</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="endPointName" /> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="endPointName" /> is empty</exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is less or equal to
		///     <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">When no network service discoverer was specified when creating this client</exception>
		public bool TryConnect(string endPointName, TimeSpan timeout)
		{
			Exception unused;
			ConnectionId unused2;
			return TryConnect(endPointName, timeout, out unused, out unused2);
		}

		/// <summary>
		///     Tries to connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns>True when the connection succeeded, false otherwise</returns>
		public bool TryConnect(IPEndPoint endPoint)
		{
			return TryConnect(endPoint, TimeSpan.FromSeconds(value: 1));
		}

		/// <summary>
		///     Tries to connect this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">
		///     The amount of time this method should block and await a successful connection from the remote
		///     end-point
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		public bool TryConnect(IPEndPoint endPoint, TimeSpan timeout)
		{
			if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));
			if (Equals(endPoint, LocalEndPoint))
				throw new ArgumentException("An endPoint cannot be connected to itself", nameof(endPoint));
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
			if (IsConnected)
				throw new InvalidOperationException(
				                                    "This endPoint is already connected to another endPoint and cannot establish any more connections");

			Exception unused;
			ConnectionId unused2;
			return TryConnect(endPoint, timeout, out unused, out unused2);
		}

		/// <summary>
		///     Tries to connect this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">
		///     The amount of time this method should block and await a successful connection from the remote
		///     end-point
		/// </param>
		/// <param name="connectionId"></param>
		/// <param name="exception"></param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		public bool TryConnect(IPEndPoint endPoint,
		                       TimeSpan timeout,
		                       out Exception exception,
		                       out ConnectionId connectionId)
		{
			if (endPoint == null) throw new ArgumentNullException(nameof(endPoint));
			if (Equals(endPoint, LocalEndPoint))
				throw new ArgumentException("An endPoint cannot be connected to itself", nameof(endPoint));
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
			if (IsConnected)
				throw new InvalidOperationException(
				                                    "This endPoint is already connected to another endPoint and cannot establish any more connections");

			Log.DebugFormat("{0}: Trying to connect to '{1}', timeout: {2}ms",
			                Name,
			                endPoint,
			                timeout.TotalMilliseconds);

			var success = false;
			ISocket socket = null;
			try
			{
				var started = DateTime.Now;
				var task = new Task<Exception>(() =>
				{
					try
					{
						Log.DebugFormat("Task to connect to '{0}' started", endPoint);

						socket = new Socket2(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
						{
							ExclusiveAddressUse = true,
							Blocking = true
						};

						Log.DebugFormat("{0}: EndPoint connecting to remote endpoint '{1}'", Name, endPoint);

						socket.Connect(endPoint);

						Log.DebugFormat("{0}: EndPoint successfully connected to remote endpoint '{1}'", Name, endPoint);

						return null;
					}
					catch (SocketException e)
					{
						return e;
					}
					catch (Exception e)
					{
						Log.WarnFormat("{0}: Caught unexpected exception while trying to connect to socket: {1}",
						               Name,
						               e);
						return e;
					}
				}, TaskCreationOptions.LongRunning);
				task.Start();
				if (!task.Wait(timeout))
				{
					exception = new NoSuchIPEndPointException(endPoint, timeout);
					CurrentConnectionId = connectionId = ConnectionId.None;
					return false;
				}

				if (task.Result != null)
				{
					exception = new NoSuchIPEndPointException(endPoint, task.Result);
					CurrentConnectionId = connectionId = ConnectionId.None;
					return false;
				}

				var remaining = timeout - (DateTime.Now - started);
				ErrorType errorType;
				string error;
				if (!TryPerformOutgoingHandshake(socket, remaining, out errorType, out error, out connectionId))
				{
					switch (errorType)
					{
						case ErrorType.Handshake:
							exception = new HandshakeException(error);
							break;

						case ErrorType.AuthenticationRequired:
							exception = new AuthenticationRequiredException(error);
							break;

						default:
							exception = new AuthenticationException(error);
							break;
					}
					CurrentConnectionId = connectionId;
					return false;
				}

				RemoteEndPoint = endPoint;
				LocalEndPoint = (IPEndPoint) socket.LocalEndPoint;

				Log.InfoFormat("{0}: EndPoint successfully connected to '{1}'", Name, endPoint);

				FireOnConnected(endPoint, CurrentConnectionId);

				success = true;
				exception = null;
				return true;
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

					RemoteEndPoint = null;
				}
			}
		}

		/// <summary>
		///     Connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="timeout"></param>
		/// <exception cref="ArgumentException">
		///     In case <paramref name="endPointName" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">
		///     When no such endPoint could be *found* - it might exist but this one is
		///     incapable of establishing a successfuly connection
		/// </exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="EndPointType.Server" />
		///     - The given endPoint failed authentication
		/// </exception>
		public ConnectionId Connect(string endPointName, TimeSpan timeout)
		{
			if (endPointName == null) throw new ArgumentNullException(nameof(endPointName));
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
			if (_networkServiceDiscoverer == null)
				throw new
					InvalidOperationException("No discoverer was specified when creating this client and thus network service discovery by name is not possible");

			var results = _networkServiceDiscoverer.FindServices(endPointName);

			if (results == null || results.Count == 0)
				throw new NoSuchIPEndPointException(endPointName);

			Exception e = null;
			foreach (var result in results)
			{
				ConnectionId connectionId;
				if (TryConnect(result.EndPoint, timeout, out e, out connectionId))
					return connectionId;
			}
			throw e;
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
		/// <exception cref="NoSuchIPEndPointException">
		///     When no such endPoint could be *found* - it might exist but this one is
		///     incapable of establishing a successfuly connection
		/// </exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="EndPointType.Server" />
		///     - The given endPoint failed authentication
		/// </exception>
		public ConnectionId Connect(IPEndPoint endPoint)
		{
			return Connect(endPoint, TimeSpan.FromSeconds(value: 1));
		}

		/// <summary>
		///     Connects this endPoint to the given one.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">
		///     The amount of time this method should block and await a successful connection from the remote
		///     end-point
		/// </param>
		/// <exception cref="ArgumentNullException">
		///     When <paramref name="endPoint" /> is null
		/// </exception>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When <paramref name="timeout" /> is equal or less than <see cref="TimeSpan.Zero" />
		/// </exception>
		/// <exception cref="InvalidOperationException">
		///     When this endPoint is already connected to another endPoint.
		/// </exception>
		/// <exception cref="NoSuchIPEndPointException">
		///     When no such endPoint could be *found* - it might exist but this one is
		///     incapable of establishing a successfuly connection
		/// </exception>
		/// <exception cref="AuthenticationException">
		///     - The given endPoint is no <see cref="EndPointType.Server" />
		///     - The given endPoint failed authentication
		/// </exception>
		/// <exception cref="AuthenticationRequiredException">
		///     - The given endPoint requires authentication, but this one didn't provide any
		/// </exception>
		/// <exception cref="HandshakeException">
		///     - The handshake between this and the given endpoint failed
		/// </exception>
		public ConnectionId Connect(IPEndPoint endPoint, TimeSpan timeout)
		{
			Exception e;
			ConnectionId connectionId;
			if (!TryConnect(endPoint, timeout, out e, out connectionId))
				throw e;

			return connectionId;
		}

		/// <summary>
		///     IPAddress+Port pair of the connected endPoint in case <see cref="SocketEndPoint.Connect(IPEndPoint)" /> has been
		///     called.
		///     Otherwise null.
		/// </summary>
		public new IPEndPoint RemoteEndPoint { get; private set; }

		/// <summary>
		///     IPAddress+Port pair of this endPoint in case <see cref="SocketEndPoint.Bind(IPAddress)" />
		///     or
		///     has been called.
		///     Otherwise null.
		/// </summary>
		public new IPEndPoint LocalEndPoint { get; private set; }

		/// <summary>
		///     Binds this socket
		/// </summary>
		/// <param name="ep"></param>
		public void Bind(IPEndPoint ep)
		{
			if (ep == null) throw new ArgumentNullException(nameof(ep));

			var socket = new Socket2(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
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

		/// <summary>
		///     Binds this endpoint endpoint to the given socket.
		/// </summary>
		/// <param name="serverSocket"></param>
		internal void Bind(ISocket serverSocket)
		{
			if (IsConnected)
				throw new InvalidOperationException("A socket may only bound to a particular port when its not already connected");

			_serverSocket = serverSocket;
			LocalEndPoint = (IPEndPoint) serverSocket.LocalEndPoint;
			Listen();
		}

		private bool TryConnect(string endPointName, TimeSpan timeout, out Exception exception, out ConnectionId connectionId)
		{
			if (endPointName == null) throw new ArgumentNullException(nameof(endPointName));
			if (endPointName == "") throw new ArgumentException("endPointName");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
			if (_networkServiceDiscoverer == null)
				throw new
					InvalidOperationException("No discoverer was specified when creating this client and thus network service discovery by name is not possible");

			var results = _networkServiceDiscoverer.FindServices(endPointName);
			if (results.Count == 0)
			{
				exception = new NoSuchIPEndPointException(endPointName);
				connectionId = ConnectionId.None;
				return false;
			}

			foreach (var result in results)
				if (TryConnect(result.EndPoint, timeout, out exception, out connectionId))
					return true;

			exception = new NoSuchIPEndPointException(endPointName);
			connectionId = ConnectionId.None;
			return false;
		}

		/// <summary>
		///     Connects to another endPoint with the given name.
		/// </summary>
		/// <param name="endPointName"></param>
		public ConnectionId Connect(string endPointName)
		{
			return Connect(endPointName, TimeSpan.FromSeconds(value: 1));
		}

		/// <inheritdoc />
		protected override void DisposeAdditional()
		{
			_serverSocket.TryDispose();
			_peerNameRegistration?.TryDispose();
		}

		/// <inheritdoc />
		protected override void Send(ISocket socket, byte[] data, int offset, int size)
		{
			socket.Send(data, offset, size, SocketFlags.None);
		}

		/// <inheritdoc />
		protected override bool SynchronizedWrite(ISocket socket, byte[] data, int length, out SocketError err)
		{
			if (!socket.Connected)
			{
				err = SocketError.NotConnected;
				return false;
			}

			var written = socket.Send(data, offset: 0, size: length, socketFlags: SocketFlags.None, errorCode: out err);
			if (written != length || err != SocketError.Success || !socket.Connected)
			{
				Log.DebugFormat("{0}: Error while writing to socket, {1} out of {2} written, method {3}, IsConnected: {4}",
				                Name,
				                written,
				                data.Length, err, socket.Connected);
				return false;
			}

			return true;
		}

		/// <inheritdoc />
		protected override bool SynchronizedRead(ISocket socket, byte[] buffer, TimeSpan timeout, out SocketError err)
		{
			var start = DateTime.Now;
			while (socket.Available < buffer.Length)
			{
				if (!socket.Connected)
				{
					err = SocketError.NotConnected;
					Log.DebugFormat("{0}: Error while reading from socket, {1} out of {2} read, method {3}, IsConnected: {4}",
					                Name,
					                0,
					                buffer.Length, err, socket.Connected);
					return false;
				}

				var remaining = timeout - (DateTime.Now - start);
				if (remaining <= TimeSpan.Zero)
				{
					err = SocketError.TimedOut;
					Log.DebugFormat("{0}: Error while reading from socket, {1} out of {2} read, method {3}, IsConnected: {4}",
					                Name,
					                0,
					                buffer.Length, err, socket.Connected);
					return false;
				}

				var t = (int) (remaining.TotalMilliseconds * 1000);
				if (!socket.Poll(t, SelectMode.SelectRead))
				{
					err = SocketError.TimedOut;
					Log.DebugFormat("{0}: Error while reading from socket, {1} out of {2} read, method {3}, IsConnected: {4}",
					                Name,
					                0,
					                buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return SynchronizedRead(socket, buffer, out err);
		}

		/// <inheritdoc />
		protected override bool SynchronizedRead(ISocket socket, byte[] buffer, out SocketError err)
		{
			err = SocketError.Success;

			var index = 0;
			int toRead;
			while ((toRead = buffer.Length - index) > 0)
			{
				var read = socket.Receive(buffer, index, toRead, SocketFlags.None, out err);
				index += read;

				if (err != SocketError.Success || read <= 0 || !socket.Connected)
				{
					Log.DebugFormat("{0}: Error while reading from socket, {1} out of {2} read, method {3}, IsConnected: {4}",
					                Name,
					                read,
					                buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return true;
		}

		/// <inheritdoc />
		protected override EndPoint GetRemoteEndPointOf(ISocket socket)
		{
			var remoteEndPoint = socket.RemoteEndPoint;
			return remoteEndPoint;
		}

		/// <inheritdoc />
		protected override void DisconnectTransport(ISocket socket, bool reuseSocket)
		{
			socket.Disconnect(reuseSocket: false);
		}

		/// <inheritdoc />
		protected override void DisposeAfterDisconnect(ISocket socket)
		{
			socket.TryDispose();
		}

		/// <summary>
		///     Sends a goodbye message over the socket.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="rpcId"></param>
		/// <param name="waitTime"></param>
		/// <returns>True when the goodbye message could be sent, false otherwise</returns>
		protected override bool SendGoodbye(ISocket socket, long rpcId, TimeSpan waitTime)
		{
			var task = new Task(() =>
			{
				try
				{
					const int messageSize = 9;

					using (var stream = new MemoryStream())
					using (var writer = new BinaryWriter(stream, Encoding.UTF8))
					{
						writer.Write(messageSize);
						writer.Write(rpcId);
						writer.Write((byte) MessageType.Goodbye);

						writer.Flush();
						stream.Position = 0;

						Send(socket, stream.GetBuffer(), offset: 0, size: messageSize + 4);
					}
				}
				catch (SocketException)
				{
				}
				catch (IOException)
				{
				}
				catch (ObjectDisposedException)
				{
				}
			});
			task.ContinueWith(t =>
			{
				if (t.IsFaulted)
					Log.ErrorFormat("Caught unhandled exception while sending goodbye: {0}", t.Exception);
			});
			task.Start();

			if (!task.Wait(waitTime))
			{
				Log.WarnFormat("Could not send goodbye message in {0}s, performing hard disconnect",
				               waitTime.TotalSeconds);
				return false;
			}

			return true;
		}

		private void Listen()
		{
			_serverSocket.Listen(backlog: 1);
			_serverSocket.BeginAccept(OnIncomingConnection, state: null);
			Log.InfoFormat("{0}: EndPoint listening on {1}", Name, LocalEndPoint);

			if (Name != null && _networkServiceDiscoverer != null)
			{
				_peerNameRegistration = _networkServiceDiscoverer.RegisterService(Name, LocalEndPoint);
				Log.InfoFormat("{0}: Endpoint '{1}' published to local cloud", Name, LocalEndPoint);
			}
		}

		private void OnIncomingConnection(IAsyncResult ar)
		{
			lock (SyncRoot)
			{
				if (IsDisposed)
					return;
			}

			ISocket socket = null;
			var success = false;
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
							socket.Disconnect(reuseSocket: false);
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
						_serverSocket.BeginAccept(OnIncomingConnection, state: null);
				}
			}
		}

		internal static ISocket CreateSocketAndBindToAnyPort(IPAddress address, out IPEndPoint localAddress)
		{
			const ushort firstSocket = 49152;
			const ushort lastSocket = 65535;

			return CreateSocketAndBindToAnyPort(address, firstSocket, lastSocket, out localAddress);
		}

		internal static ISocket CreateSocketAndBindToAnyPort(IPAddress address,
		                                                    ushort firstSocket,
		                                                    ushort lastSocket,
		                                                    out IPEndPoint localAddress)
		{
			var family = address.AddressFamily;
			var socket = new Socket2(family, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				socket.ExclusiveAddressUse = true;

				localAddress = null;
				for (var i = firstSocket; i <= lastSocket; ++i)
					try
					{
						localAddress = new IPEndPoint(address, i);
						socket.Bind(localAddress);
						break;
					}
					catch (SocketException)
					{
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