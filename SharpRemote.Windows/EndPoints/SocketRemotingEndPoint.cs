using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.PeerToPeer;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.CodeGeneration;
using SharpRemote.CodeGeneration.Serialization;
using log4net;

// ReSharper disable CheckNamespace

namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public sealed class SocketRemotingEndPoint
		: AbstractEndPoint
		  , IRemotingEndPoint
		  , IEndPointChannel
	{
		private const string ServerWelcomeMessage = "SharpRemote Socket";
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly byte[] ServerWelcomeMessageData = Encoding.UTF8.GetBytes(ServerWelcomeMessage);
		private static readonly int ServerWelcomeMessageLength = ServerWelcomeMessageData.Length;
		private readonly AssemblyBuilder _assembly;
		private readonly ModuleBuilder _module;

		private readonly string _name;
		private readonly Dictionary<long, Action<MessageType, BinaryReader>> _pendingCalls;
		private readonly HashSet<MethodInvocation> _pendingInvocations;
		private readonly Dictionary<ulong, IProxy> _proxies;
		private readonly ProxyCreator _proxyCreator;
		private readonly Serializer _serializer;
		private readonly ServantCreator _servantCreator;
		private readonly Dictionary<ulong, IServant> _servants;
		private readonly object _syncRoot;
		private CancellationTokenSource _cancellationTokenSource;
		private bool _isDisposed;
		private IPEndPoint _localEndPoint;
		private long _nextRpcId;
		private Task _readTask;
		private IPEndPoint _remoteEndPoint;
		private Socket _serverSocket;
		private Socket _socket;
		private PeerNameRegistration _peerNameRegistration;

		public SocketRemotingEndPoint(string name = null)
		{
			_syncRoot = new object();

			_name = name ?? "<Unnamed>";

			_servants = new Dictionary<ulong, IServant>();
			_proxies = new Dictionary<ulong, IProxy>();

			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			string moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(moduleName);

			_serializer = new Serializer(_module);
			_servantCreator = new ServantCreator(_module, _serializer, this, this);
			_proxyCreator = new ProxyCreator(_module, _serializer, this, this);
			_pendingCalls = new Dictionary<long, Action<MessageType, BinaryReader>>();
			_pendingInvocations = new HashSet<MethodInvocation>();
		}

		#region Reading from / Writing to socket

		private void Read(object sock)
		{
			var pair = (KeyValuePair<Socket, CancellationToken>) sock;
			Socket socket = pair.Key;
			CancellationToken token = pair.Value;

			try
			{
				var size = new byte[4];
				while (true)
				{
					if (token.IsCancellationRequested)
					{
						Log.InfoFormat("Cancellation was requested: Stopping read and disconnecting '{0}'", _name);
						break;
					}

					SocketError err;
					if (!SynchronizedRead(socket, size, out err))
						break;

					int length = BitConverter.ToInt32(size, 0);
					if (length >= 8)
					{
						var buffer = new byte[length];
						if (!SynchronizedRead(socket, buffer, out err))
							break;

						var stream = new MemoryStream(buffer, false);
						var reader = new BinaryReader(stream);
						long rpcId = reader.ReadInt64();
						var type = (MessageType) reader.ReadByte();
						HandleMessage(rpcId, type, reader);
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

		private bool SynchronizedWrite(Socket socket, byte[] data, int length, out SocketError err)
		{
			lock (_syncRoot)
			{
				if (!socket.Connected)
				{
					err = SocketError.NotConnected;
					return false;
				}

				int written = socket.Send(data, 0, length, SocketFlags.None, out err);
				if (written != length || err != SocketError.Success || !socket.Connected)
				{
					Log.ErrorFormat("Error while writing to socket: {0} out of {1} written, method {2}, IsConnected: {3}", written,
					                data.Length, err, socket.Connected);
					return false;
				}

				return true;
			}
		}

		private bool SynchronizedRead(Socket socket, byte[] buffer, TimeSpan timeout, out SocketError err)
		{
			DateTime start = DateTime.Now;
			while (socket.Available < buffer.Length)
			{
				TimeSpan remaining = timeout - (DateTime.Now - start);
				var t = (int) (remaining.TotalMilliseconds*1000);
				if (!socket.Poll(t, SelectMode.SelectRead))
				{
					err = SocketError.TimedOut;
					Log.ErrorFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", 0,
					                buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return SynchronizedRead(socket, buffer, out err);
		}

		private bool SynchronizedRead(Socket socket, byte[] buffer, out SocketError err)
		{
			err = SocketError.Success;

			int index = 0;
			int toRead;
			while ((toRead = buffer.Length - index) > 0)
			{
				int read = socket.Receive(buffer, index, toRead, SocketFlags.None, out err);
				index += read;

				if (err != SocketError.Success || read == 0 || !socket.Connected)
				{
					Log.ErrorFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", read,
					                buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return true;
		}

		#endregion

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

		public MemoryStream CallRemoteMethod(ulong servantId, string methodName, MemoryStream arguments)
		{
			Socket socket = _socket;
			if (socket == null || !socket.Connected)
				throw new NotConnectedException(_name);

			long rpcId = Interlocked.Increment(ref _nextRpcId);
			int messageLength;
			byte[] message = CreateMessage(servantId, methodName, arguments, rpcId, out messageLength);

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
					if (!SynchronizedWrite(socket, message, messageLength, out err))
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
						var e = (Exception) formatter.Deserialize(response.BaseStream);
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

		public void Dispose()
		{
			lock (_syncRoot)
			{
				Disconnect();
				_serverSocket.TryDispose();
				_isDisposed = true;

				if (_peerNameRegistration != null)
				{
					_peerNameRegistration.Dispose();
				}
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public bool IsConnected
		{
			get { return _remoteEndPoint != null; }
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
					{
					}

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
				_proxies.Add(objectId, (IProxy) proxy);
				return proxy;
			}
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			IProxy proxy;
			if (!_proxies.TryGetValue(objectId, out proxy))
				throw new ArgumentException(string.Format("No such proxy: {0}", objectId));

			if (!(proxy is T))
				throw new ArgumentException(string.Format("The proxy '{0}', {1} is not related to interface: {2}",
				                                          objectId,
				                                          proxy.GetType().Name,
				                                          typeof (T).Name));

			return (T) proxy;
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

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			throw new NotImplementedException();
		}

		private void OnIncomingConnection(IAsyncResult ar)
		{
			lock (_syncRoot)
			{
				if (_isDisposed)
					return;

				try
				{
					OnConnected(_serverSocket.EndAccept(ar));
					_socket.Send(ServerWelcomeMessageData);
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
			lock (_syncRoot)
			{
				_socket = socket;
				_remoteEndPoint = (IPEndPoint) socket.RemoteEndPoint;
				_cancellationTokenSource = new CancellationTokenSource();
				_readTask = new Task(Read, new KeyValuePair<Socket, CancellationToken>(_socket, _cancellationTokenSource.Token));
				_readTask.Start();

				Log.InfoFormat("{0}: Connected to {1}", _name, _remoteEndPoint);
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
			Log.InfoFormat("EndPoint '{0}' listening on {1}", _name, _localEndPoint);

			if (_name != null)
			{
				var peerName = new PeerName(_name, PeerNameType.Unsecured);
				_peerNameRegistration = new PeerNameRegistration
				{
					PeerName = peerName,
					Port = _localEndPoint.Port,
				};
				_peerNameRegistration.Start();
				Log.InfoFormat("Endpoint '{0}@{1}' published to local cloud via PNRP", _name, _localEndPoint);
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

		private bool ReadWelcomeMessage(Socket socket, TimeSpan remaining, EndPoint ep)
		{
			var buffer = new byte[ServerWelcomeMessageLength];
			SocketError err;
			if (!SynchronizedRead(socket, buffer, remaining, out err))
			{
				Log.ErrorFormat("EndPoint '{0}' failed to respond with welcome message in time, disconnecting...", ep);
				return false;
			}

// ReSharper disable LoopCanBeConvertedToQuery
			for (int i = 0; i < buffer.Length; ++i)
// ReSharper restore LoopCanBeConvertedToQuery
			{
				if (buffer[i] != ServerWelcomeMessageData[i])
				{
					Log.ErrorFormat("EndPoint '{0}' answered with wrong welcome message, disconnecting...", ep);
					return false;
				}
			}

			return true;
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
						exceptionLength = (int) stream.Length;
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

		public override string ToString()
		{
			return _name;
		}

		private void HandleMessage(long rpcId, MessageType type, BinaryReader reader)
		{
			if (type == MessageType.Call)
			{
				HandleRequest(rpcId, reader);
			}
			else if ((type & MessageType.Return) != 0)
			{
				if (!HandleResponse(rpcId, type, reader))
				{
					Log.ErrorFormat("There is no pending RPC of id '{0}' - disconnecting...", rpcId);
					Disconnect();
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private void DispatchMethodInvocation(long rpcId, IGrain grain, string methodName, BinaryReader reader)
		{
			var task = new Task(() =>
				{
					try
					{
						Socket socket = _socket;
						var response = new MemoryStream();
						var writer = new BinaryWriter(response, Encoding.UTF8);
						try
						{
							WriteResponseHeader(rpcId, writer, MessageType.Return);
							grain.Invoke(methodName, reader, writer);
							PatchResponseMessageLength(response, writer);
						}
						catch (Exception e)
						{
							response.Position = 0;
							WriteResponseHeader(rpcId, writer, MessageType.Return | MessageType.Exception);
							WriteException(writer, e);
							PatchResponseMessageLength(response, writer);
						}

						var responseLength = (int) response.Length;
						byte[] data = response.GetBuffer();

						SocketError err;
						if (!SynchronizedWrite(socket, data, responseLength, out err))
						{
							Log.ErrorFormat("Disconnecting socket due to error while writing response!");
							Disconnect();
						}
					}
					catch (Exception e)
					{
						Log.FatalFormat("Caught exception while dispatching method invocation, disconnecting: {0}", e);
						Disconnect();
					}
				});
			var methodInvocation = new MethodInvocation(rpcId, grain, methodName, task);

			task.ContinueWith(unused => { _pendingInvocations.Remove(methodInvocation); });
			_pendingInvocations.Add(methodInvocation);
			task.Start();
		}

		private void HandleRequest(long rpcId, BinaryReader reader)
		{
			ulong servantId = reader.ReadUInt64();
			string methodName = reader.ReadString();

			IServant servant;
			lock (_servants)
			{
				_servants.TryGetValue(servantId, out servant);
			}

			if (servant != null)
			{
				DispatchMethodInvocation(rpcId, servant, methodName, reader);
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
					DispatchMethodInvocation(rpcId, proxy, methodName, reader);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
		}

		private static void PatchResponseMessageLength(MemoryStream response, BinaryWriter writer)
		{
			var bufferSize = (int) response.Length;
			int messageSize = bufferSize - 4;
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

		private static byte[] CreateMessage(ulong servantId, string methodName, MemoryStream arguments, long rpcId,
		                                    out int bufferSize)
		{
			int argumentSize = arguments != null ? (int) arguments.Length : 0;
			int maxMessageSize = 1 + 8 + 8 + methodName.Length*2 + argumentSize;
			int maxBufferSize = 4 + maxMessageSize;
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
					byte[] data = arguments.GetBuffer();
					var dataLength = (int) arguments.Length;
					writer.Write(data, 0, dataLength);
				}

				writer.Flush();

				bufferSize = (int) stream.Position;
				int messageSize = bufferSize - 4;
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

		[Flags]
		private enum MessageType : byte
		{
			Call = 0x1,
			Return = 0x2,
			Exception = 0x4,
		}
	}
}