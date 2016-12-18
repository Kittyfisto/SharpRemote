using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SharpRemote.Extensions;
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
		, ISocketRemotingEndPoint
	{
		private static new readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

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
				Log.DebugFormat("{0}: Error while writing to socket, {1} out of {2} written, method {3}, IsConnected: {4}",
				                Name,
				                written,
				                data.Length, err, socket.Connected);
				return false;
			}

			return true;
		}

		protected override bool SynchronizedRead(Socket socket, byte[] buffer, TimeSpan timeout, out SocketError err)
		{
			DateTime start = DateTime.Now;
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

				TimeSpan remaining = timeout - (DateTime.Now - start);
				if (remaining <= TimeSpan.Zero)
				{
					err = SocketError.TimedOut;
					Log.DebugFormat("{0}: Error while reading from socket, {1} out of {2} read, method {3}, IsConnected: {4}",
					                Name,
					                0,
					                buffer.Length, err, socket.Connected);
					return false;
				}

				var t = (int)(remaining.TotalMilliseconds * 1000);
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
					Log.DebugFormat("{0}: Error while reading from socket, {1} out of {2} read, method {3}, IsConnected: {4}", read,
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

		protected override void DisposeAfterDisconnect(Socket socket)
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
		protected override bool SendGoodbye(Socket socket, long rpcId, TimeSpan waitTime)
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
						writer.Write((byte)MessageType.Goodbye);

						writer.Flush();
						stream.Position = 0;

						Send(socket, stream.GetBuffer(), 0, messageSize + 4);
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
				{
					Log.ErrorFormat("Caught unhandled exception while sending goodbye: {0}", t.Exception);
				}
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

	}
}