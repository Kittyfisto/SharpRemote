using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     <see cref="IRemotingEndPoint" /> implementation that establishes a TCP socket with another
	///     endPoint. A listening socket is opened (and bound to an address) with <see cref="SocketRemotingEndPointServer.Bind(IPAddress)" /> while
	///     a connectiong to such a socket is established with <see cref="SocketRemotingEndPointClient.Connect(IPEndPoint)" /> or
	///     <see cref="SocketRemotingEndPointClient.Connect(string)" />.
	/// </summary>
	public abstract class AbstractIPSocketRemotingEndPoint
		: AbstractBinaryStreamEndPoint<Socket>
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private int _previousConnectionId;
		private Thread _readThread;
		private Thread _writeThread;
		private IPEndPoint _remoteEndPoint;
		private IPEndPoint _localEndPoint;

		internal AbstractIPSocketRemotingEndPoint(EndPointType type,
		                                          string name = null,
		                                          IAuthenticator clientAuthenticator = null,
		                                          IAuthenticator serverAuthenticator = null,
		                                          ITypeResolver customTypeResolver = null,
		                                          Serializer serializer = null,
		                                          HeartbeatSettings heartbeatSettings = null,
		                                          LatencySettings latencySettings = null,
		                                          EndPointSettings endPointSettings = null)
			: base(new GrainIdGenerator(type),
			       name,
			       type,
			       clientAuthenticator,
			       serverAuthenticator,
			       customTypeResolver,
			       serializer,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{
			_previousConnectionId = 0;
		}

		/// <summary>
		///     IPAddress+Port pair of the connected endPoint in case <see cref="SocketRemotingEndPointClient.Connect(IPEndPoint)" /> has been called.
		///     Otherwise null.
		/// </summary>
		public new IPEndPoint RemoteEndPoint
		{
			get { return _remoteEndPoint; }
			protected set { _remoteEndPoint = value; }
		}

		protected override EndPoint InternalRemoteEndPoint
		{
			get { return _remoteEndPoint; }
			set { _remoteEndPoint = (IPEndPoint)value; }
		}

		/// <summary>
		///     IPAddress+Port pair of this endPoint in case <see cref="SocketRemotingEndPointServer.Bind(IPAddress)" /> 
		/// or 
		/// has been called.
		///     Otherwise null.
		/// </summary>
		public new IPEndPoint LocalEndPoint
		{
			get { return _localEndPoint; }
			protected set { _localEndPoint = value; }
		}

		protected override EndPoint InternalLocalEndPoint
		{
			get { return _localEndPoint; }
		}

		protected override void Send(Socket socket, byte[] data, int offset, int size)
		{
			socket.Send(data, offset, size, SocketFlags.None);
		}

		protected override bool SynchronizedWrite(Socket socket, byte[] data, int length, out SocketError err)
		{
			if (!socket.Connected)
			{
				err = SocketError.NotConnected;
				return false;
			}

			int written = socket.Send(data, 0, length, SocketFlags.None, out err);
			if (written != length || err != SocketError.Success || !socket.Connected)
			{
				Log.DebugFormat("Error while writing to socket: {0} out of {1} written, method {2}, IsConnected: {3}", written,
								data.Length, err, socket.Connected);
				return false;
			}

			return true;
		}

		protected override bool SynchronizedRead(Socket socket, byte[] buffer, System.TimeSpan timeout, out SocketError err)
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

				var t = (int)(remaining.TotalMilliseconds * 1000);
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

		protected override bool SynchronizedRead(Socket socket, byte[] buffer, out SocketError err)
		{
			err = SocketError.Success;

			int index = 0;
			int toRead;
			while ((toRead = buffer.Length - index) > 0)
			{
				int read = socket.Receive(buffer, index, toRead, SocketFlags.None, out err);
				index += read;

				if (err != SocketError.Success || read <= 0 || !socket.Connected)
				{
					Log.DebugFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", read,
									buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return true;
		}

		protected override EndPoint GetRemoteEndPointOf(Socket socket)
		{
			EndPoint remoteEndPoint = socket.RemoteEndPoint;
			return remoteEndPoint;
		}

		protected override void DisconnectTransport(Socket socket, bool reuseSocket)
		{
			socket.Disconnect(false);
		}

		protected override ConnectionId OnHandshakeSucceeded(Socket socket)
		{
			lock (SyncRoot)
			{
				// There is possibly still a possibility that the _pendingMethodInvocations dictionary
				// contains some entries, EVEN though it's cleared upon being disconnected.
				// For the sake of stability, we'll clear it here again, BEFORE starting
				// the read/write threads, so we most certainly start with a clean slate (once again).
				ClearPendingMethodInvocations();

				Socket = socket;
				_remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
				CurrentConnectionId = new ConnectionId(Interlocked.Increment(ref _previousConnectionId));
				CancellationTokenSource = new CancellationTokenSource();

				var args = new ThreadArgs(socket, CancellationTokenSource.Token, CurrentConnectionId);

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

				Log.InfoFormat("{0}: Connected to {1}", Name, _remoteEndPoint);

				return CurrentConnectionId;
			}
		}
	}
}