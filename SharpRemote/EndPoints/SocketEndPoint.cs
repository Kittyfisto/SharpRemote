using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.CodeGeneration;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public sealed class SocketEndPoint
		: AbstractEndPoint
		, IRemotingEndPoint
		, IEndPointChannel
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly object _syncRoot;
		private readonly IPEndPoint _localEndPoint;
		private readonly string _name;
		private readonly Socket _serverSocket;
		private readonly Dictionary<ulong, IProxy> _proxies;
		private readonly Dictionary<ulong, IServant> _servants;
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private Task _readTask;
		private IPEndPoint _remoteEndPoint;
		private Socket _socket;
		private long _nextRpcId;

		private readonly Dictionary<long, Action<MessageType, BinaryReader>> _pendingCalls;
		private readonly string _stacktrace;

		[Flags]
		private enum MessageType : byte
		{
			Call = 0x1,
			Return = 0x2,
			Exception = 0x4,
		}

		public SocketEndPoint(IPAddress localAddress, string name = null)
		{
			if (localAddress == null) throw new ArgumentNullException("localAddress");

			_syncRoot = new object();

			_name = name ?? "<Unnamed>";
			_stacktrace = new StackTrace().ToString();

			_serverSocket = CreateSocketAndBindToAnyPort(localAddress, out _localEndPoint);
			_serverSocket.Listen(1);
			_serverSocket.BeginAccept(OnIncomingConnection, null);

			Log.InfoFormat("Socket '{0}' listening on {1}", _name, _localEndPoint);

			_servants = new Dictionary<ulong, IServant>();
			_proxies = new Dictionary<ulong, IProxy>();

			_cancellationTokenSource = new CancellationTokenSource();

			_servantCreator = new ServantCreator(this);
			_proxyCreator = new ProxyCreator(this);
			_pendingCalls = new Dictionary<long, Action<MessageType, BinaryReader>>();
		}

		private void Read(object sock)
		{
			var pair = (KeyValuePair<Socket, CancellationToken>) sock;
			var socket = pair.Key;
			var token = pair.Value;

			try
			{
				var size = new byte[4];
				while (!token.IsCancellationRequested)
				{
					SocketError err;
					if (SynchronizedRead(socket, size, out err) != size.Length || !HandleError(socket, err))
						break;

					var length = BitConverter.ToInt32(size, 0);
					if (length >= 8)
					{
						var buffer = new byte[length];
						if (SynchronizedRead(socket, buffer, out err) != buffer.Length || !HandleError(socket, err))
							break;

						var stream = new MemoryStream(buffer, false);
						var reader = new BinaryReader(stream);
						var rpcId = reader.ReadInt64();
						var type = (MessageType)reader.ReadByte();
						int responseLength;
						var response = HandleMessage(rpcId, type, reader, out responseLength);
						if (response != null)
						{
							if (SynchronizedWrite(socket, response, responseLength, out err) != responseLength || !HandleError(socket, err))
								break;
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught exception while reading/handling messages: {0}", e);
			}

			Disconnect();
		}

		#region Reading from / Writing to socket
		
		private int SynchronizedWrite(Socket socket, byte[] response, int length, out SocketError err)
		{
			lock (_syncRoot)
			{
				if (!socket.Connected)
				{
					err = SocketError.NotConnected;
					return -1;
				}

				return socket.Send(response, 0, length, SocketFlags.None, out err);
			}
		}

		private int SynchronizedRead(Socket socket, byte[] data, out SocketError err)
		{
			var read = socket.Receive(data, 0, data.Length, SocketFlags.None, out err);
			if (err != SocketError.Success || !IsSocketConnected(socket))
				return -1;

			return read;
		}

		#endregion

		private static bool IsSocketConnected(Socket socket)
		{
			// let's find out of the socket was interrupted
			try
			{
				if (socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0)
				{
					return false;
				}

				return true;
			}
			catch (SocketException)
			{
				return false;
			}
		}

		private bool HandleError(Socket socket, SocketError err)
		{
			switch (err)
			{
				case SocketError.Success:
					if (!IsSocketConnected(socket))
					{
						Log.InfoFormat("Socket '{0}' connection aborted", _name);
						return false;
					}

					return true;

				case SocketError.NotConnected:
				case SocketError.Interrupted:
					return false;

				case SocketError.ConnectionAborted:
					Log.InfoFormat("Socket '{0}' connection aborted", _name);
					return false;

				default:
					Log.ErrorFormat("Unexpected socket error '{0}' - disconnecting from other endpoint", err);
					return false;
			}
		}

		private void OnIncomingConnection(IAsyncResult ar)
		{
			try
			{
				OnConnected(_serverSocket.EndAccept(ar));
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught exception while accepting incoming connection - disconnecting again: {0}", e);
				Disconnect();
			}
		}

		private void OnConnected(Socket socket)
		{
			lock (_syncRoot)
			{
				_socket = socket;
				_remoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
				_readTask = new Task(Read, new KeyValuePair<Socket, CancellationToken>(_socket, _cancellationTokenSource.Token));
				_readTask.Start();
				Log.InfoFormat("{0}: Connected to {1}", _name, _remoteEndPoint);
			}
		}

		private Socket CreateSocketAndBindToAnyPort(IPAddress address, out IPEndPoint localAddress)
		{
			var family = address.AddressFamily;
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

		public void Dispose()
		{
			Disconnect();
		}

		public string Name
		{
			get { return _name; }
		}

		public IPEndPoint LocalEndPoint
		{
			get { return _localEndPoint; }
		}

		public IPEndPoint RemoteEndPoint
		{
			get { return _remoteEndPoint; }
		}

		public bool IsConnected
		{
			get { return _remoteEndPoint != null; }
		}

		public void Connect(IPEndPoint endPoint, TimeSpan timeout)
		{
			if (endPoint == null) throw new ArgumentNullException("endPoint");
			if (Equals(endPoint, _localEndPoint)) throw new ArgumentException("A remote endpoint cannot be connected to itself", "endPoint");
			if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException("timeout");
			if (IsConnected) throw new InvalidOperationException("This endpoint is already connected to another endpoint and cannot establish any more connections");

			var socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				var task = new Task(() => socket.Connect(endPoint));
				task.Start();
				if (!task.Wait(timeout))
					throw new NoSuchEndPointException(endPoint);

				OnConnected(socket);
			}
			catch (SocketException e)
			{
				throw new NoSuchEndPointException(endPoint, e);
			}
			catch (Exception)
			{
				socket.Dispose();
				throw;
			}
		}

		private void InterruptOngoingCalls()
		{
			lock (_pendingCalls)
			{
				if (_pendingCalls.Count > 0)
				{
					byte[] exceptionMessage;
					int exceptionLength;

					using (var stream = new MemoryStream())
					using (var writer = new BinaryWriter(stream, Encoding.UTF8))
					{
						WriteException(writer, new ConnectionLostException());
						exceptionMessage = stream.GetBuffer();
						exceptionLength = (int)stream.Length;
					}

					foreach (var call in _pendingCalls.Values)
					{
						var stream = new MemoryStream(exceptionMessage, 0, exceptionLength);
						var reader = new BinaryReader(stream, Encoding.UTF8);
						call(MessageType.Return | MessageType.Exception, reader);
					}
					_pendingCalls.Clear();
				}
			}
		}

		public void Disconnect()
		{
			lock (_syncRoot)
			{
				if (_socket != null)
				{
					Log.InfoFormat("Disconnecting socket '{0}' from {1}", _name, _remoteEndPoint);

					InterruptOngoingCalls();

					_cancellationTokenSource.Cancel();

					try
					{
						_socket.Disconnect(false);
					}
					catch (SocketException)
					{}

					_socket = null;
					_remoteEndPoint = null;
				}
			}
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			lock (_proxies)
			{
				var proxy = _proxyCreator.CreateProxy<T>(objectId);
				_proxies.Add(objectId, (IProxy)proxy);
				return proxy;
			}
		}

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			IServant servant = _servantCreator.CreateServant(objectId, subject);
			lock (_servants)
			{
				_servants.Add(objectId, servant);
			}
			return servant;
		}

		public MemoryStream CallRemoteMethod(ulong servantId, string methodName, MemoryStream arguments)
		{
			var socket = _socket;
			if (socket == null || !socket.Connected)
				throw new NotConnectedException(_name);

			var rpcId = Interlocked.Increment(ref _nextRpcId);
			int messageLength;
			var message = CreateMessage(servantId, methodName, arguments, rpcId, out messageLength);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0} to {1}: sending RPC #{2} to {3}.{4}",
					_localEndPoint,
					_remoteEndPoint,
					rpcId,
					servantId,
					methodName);
			}

			try
			{
				using (var handle = new ManualResetEvent(false))
				{
					BinaryReader response = null;
					var type = MessageType.Call;
					lock (_pendingCalls)
					{
						_pendingCalls.Add(rpcId, (t, r) =>
						{
							type = t;
							response = r;
							handle.Set();
						});
					}

					SocketError err;
					if (SynchronizedWrite(socket, message, messageLength, out err) != messageLength)
					{
						Log.ErrorFormat("Error while sending message: {0}", err);
						throw new ConnectionLostException();
					}

					if (!handle.WaitOne())
						throw new NotImplementedException();

					if (type == MessageType.Return)
					{
						return (MemoryStream) response.BaseStream;
					}
					else if ((type & MessageType.Exception) != 0)
					{
						var formatter = new BinaryFormatter();
						var e = (Exception)formatter.Deserialize(response.BaseStream);
						throw e;
					}
					else
					{
						throw new NotImplementedException();
					}
				}
			}
			finally
			{
				lock (_pendingCalls)
				{
					_pendingCalls.Remove(rpcId);
				}
			}
		}

		private byte[] HandleMessage(long rpcId, MessageType type, BinaryReader reader, out int responseLength)
		{
			if (type == MessageType.Call)
			{
				return HandleRequest(rpcId, reader, out responseLength);
			}
			if ((type & MessageType.Return) != 0)
			{
				if (!HandleResponse(rpcId, type, reader))
				{
					Log.ErrorFormat("There is no pending RPC of id '{0}' - disconnecting...", rpcId);
					Disconnect();
				}

				responseLength = -1;
				return null;
			}

			throw new NotImplementedException();
		}

		private byte[] HandleRequest(long rpcId, BinaryReader reader, out int responseLength)
		{
			var servantId = reader.ReadUInt64();
			var methodName = reader.ReadString();

			var response = new MemoryStream();
			var writer = new BinaryWriter(response, Encoding.UTF8);
			try
			{
				IServant servant;
				lock (_servants)
				{
					_servants.TryGetValue(servantId, out servant);
				}

				if (servant != null)
				{
					WriteResponseHeader(rpcId, writer, MessageType.Return);
					servant.InvokeMethod(methodName, reader, writer);
					PatchResponseMessageLength(response, writer);
				}
				else
				{
					IProxy proxy;
					lock (_proxies)
					{
						_proxies.TryGetValue(servantId, out proxy);
					}

					if (proxy != null)
					{
						WriteResponseHeader(rpcId, writer, MessageType.Return);
						proxy.InvokeEvent(methodName, reader, writer);
						PatchResponseMessageLength(response, writer);
					}
				}
			}
			catch (Exception e)
			{
				response.Position = 0;
				WriteResponseHeader(rpcId, writer, MessageType.Return | MessageType.Exception);
				WriteException(writer, e);
				PatchResponseMessageLength(response, writer);
			}

			responseLength = (int) response.Length;
			return response.GetBuffer();
		}

		private static void PatchResponseMessageLength(MemoryStream response, BinaryWriter writer)
		{
			var bufferSize = (int)response.Length;
			var messageSize = bufferSize - 4;
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
			Action<MessageType, BinaryReader> fn;
			lock (_pendingCalls)
			{
				if (!_pendingCalls.TryGetValue(rpcId, out fn))
					return false;
			}

			fn(messageType, reader);
			return true;
		}

		private static byte[] CreateMessage(ulong servantId, string methodName, MemoryStream arguments, long rpcId, out int bufferSize)
		{
			var argumentSize = arguments != null ? (int)arguments.Length : 0;
			var maxMessageSize = 1 + 8 + 8 + methodName.Length * 2 + argumentSize;
			var maxBufferSize = 4 + maxMessageSize;
			var buffer = new byte[maxBufferSize];
			using (var stream = new MemoryStream(buffer, 0, buffer.Length, true, true))
			using (var writer = new BinaryWriter(stream, Encoding.UTF8))
			{
				writer.Write(maxMessageSize);
				writer.Write(rpcId);
				writer.Write((byte) MessageType.Call);
				writer.Write(servantId);
				writer.Write(methodName);

				if (arguments != null)
				{
					var data = arguments.GetBuffer();
					var dataLength = (int) arguments.Length;
					writer.Write(data, 0, dataLength);
				}

				writer.Flush();

				bufferSize = (int)stream.Position;
				var messageSize = bufferSize - 4;
				if (messageSize < maxMessageSize)
				{
					stream.Position = 0;
					writer.Write(messageSize);
				}
				else if (messageSize > maxMessageSize)
				{
					throw new NotImplementedException("Buffer size miscalculation");
				}

				stream.Position = 0;
			}
			return buffer;
		}
	}
}