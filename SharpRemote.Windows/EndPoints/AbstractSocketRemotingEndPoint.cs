using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
	/// <summary>
	/// Base class for any <see cref="IRemotingEndPoint"/> implementation that used an underlying
	/// <see cref="Socket"/> implementation
	/// </summary>
	public abstract class AbstractSocketRemotingEndPoint
		: AbstractEndPoint
		, IRemotingEndPoint
		, IEndPointChannel
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string ServerWelcomeMessage = "SharpRemote Socket";
		private static readonly byte[] ServerWelcomeMessageData = Encoding.UTF8.GetBytes(ServerWelcomeMessage);
		private static readonly int ServerWelcomeMessageLength = ServerWelcomeMessageData.Length;

		#region Code Generation

		private readonly AssemblyBuilder _assembly;
		private readonly ModuleBuilder _module;
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;

		#endregion

		private readonly object _syncRoot;

		#region Proxies / Servants

		private readonly Dictionary<ulong, IProxy> _proxies;
		private readonly Dictionary<ulong, IServant> _servants;

		#endregion

		#region Method Invocation

		private readonly Dictionary<long, Action<MessageType, BinaryReader>> _pendingCalls;
		private readonly HashSet<MethodInvocation> _pendingInvocations;

		#endregion

		private readonly Serializer _serializer;
		private bool _isDisposed;
		private long _nextRpcId;
		private Socket _socket;
		private readonly string _name;

		protected abstract EndPoint InternalLocalEndPoint { get; }
		protected abstract EndPoint InternalRemoteEndPoint { get; set; }
		protected object SyncRoot{get { return _syncRoot; }}
		protected Socket Socket { get { return _socket; } set { _socket = value; } }

		protected AbstractSocketRemotingEndPoint(string name)
		{
			_name = name ?? "<Unnamed>";
			_syncRoot = new object();

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

		protected void Read(object sock)
		{
			var pair = (KeyValuePair<Socket, CancellationToken>)sock;
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
						var type = (MessageType)reader.ReadByte();
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
				var t = (int)(remaining.TotalMilliseconds * 1000);
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

		public MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			Socket socket = _socket;
			if (socket == null || !socket.Connected)
				throw new NotConnectedException(_name);

			long rpcId = Interlocked.Increment(ref _nextRpcId);
			int messageLength;
			byte[] message = CreateMessage(servantId, interfaceType, methodName, arguments, rpcId, out messageLength);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0} to {1}: sending RPC #{2} to {3}.{4}",
								InternalLocalEndPoint,
								InternalRemoteEndPoint,
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
						return (MemoryStream)response.BaseStream;
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

		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				Disconnect();
				DisposeAdditional();
				_isDisposed = true;
			}
		}

		protected void SendWelcomeMessage()
		{
			_socket.Send(ServerWelcomeMessageData);
		}

		protected abstract void DisposeAdditional();

		public string Name
		{
			get { return _name; }
		}

		public bool IsConnected
		{
			get { return InternalRemoteEndPoint != null; }
		}

		public void Disconnect()
		{
			lock (_syncRoot)
			{
				if (_socket != null)
				{
					Log.InfoFormat("Disconnecting socket '{0}' from {1}", _name, InternalRemoteEndPoint);

					InterruptOngoingCalls();

					

					try
					{
						_socket.Disconnect(false);
					}
					catch (SocketException)
					{
					}

					_socket = null;
					InternalRemoteEndPoint = null;
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

		public T GetProxy<T>(ulong objectId) where T : class
		{
			IProxy proxy;
			if (!_proxies.TryGetValue(objectId, out proxy))
				throw new ArgumentException(string.Format("No such proxy: {0}", objectId));

			if (!(proxy is T))
				throw new ArgumentException(string.Format("The proxy '{0}', {1} is not related to interface: {2}",
														  objectId,
														  proxy.GetType().Name,
														  typeof(T).Name));

			return (T)proxy;
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

		private void DispatchMethodInvocation(long rpcId, IGrain grain, string typeName, string methodName, BinaryReader reader)
		{
			var taskScheduler = grain.GetTaskScheduler(methodName);
			var task = new Task(() =>
			{
				try
				{
					Socket socket = _socket;
					var response = new MemoryStream();
					var writer = new BinaryWriter(response, Encoding.UTF8);
					try
					{
						EnsureTypeSafety(grain.ObjectId, grain.InterfaceType, typeName, methodName);

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

					var responseLength = (int)response.Length;
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
			task.Start(taskScheduler);
		}

		private void HandleRequest(long rpcId, BinaryReader reader)
		{
			ulong servantId = reader.ReadUInt64();
			string typeName = reader.ReadString();
			string methodName = reader.ReadString();

			IServant servant;
			lock (_servants)
			{
				_servants.TryGetValue(servantId, out servant);
			}

			if (servant != null)
			{
				DispatchMethodInvocation(rpcId, servant, typeName, methodName, reader);
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
					DispatchMethodInvocation(rpcId, proxy, typeName, methodName, reader);
				}
				else
				{
					throw new NotImplementedException();
				}
			}
		}

		private static void EnsureTypeSafety(ulong objectId, Type getType, string typeName, string methodName)
		{
			var actualTypeName = getType.FullName;
			if (actualTypeName != typeName)
			{
				throw new TypeMismatchException(
					string.Format("There was a type mismatch when invoking method '{0}' on grain #{1}: Expected '{2}' but found '{3}",
					methodName,
					objectId,
					typeName,
					actualTypeName));
			}
		}

		private static void PatchResponseMessageLength(MemoryStream response, BinaryWriter writer)
		{
			var bufferSize = (int)response.Length;
			int messageSize = bufferSize - 4;
			response.Position = 0;
			writer.Write(messageSize);
		}

		private static void WriteResponseHeader(long rpcId, BinaryWriter writer, MessageType type)
		{
			const int responseSizeStub = 0;
			writer.Write(responseSizeStub);
			writer.Write(rpcId);
			writer.Write((byte)type);
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

		private static byte[] CreateMessage(ulong servantId, string interfaceType, string methodName, MemoryStream arguments, long rpcId, out int bufferSize)
		{
			int argumentSize = arguments != null ? (int)arguments.Length : 0;
			int maxMessageSize = 1 + 8 + 8 + interfaceType.Length*2 + methodName.Length * 2 + argumentSize;
			int maxBufferSize = 4 + maxMessageSize;
			var buffer = new byte[maxBufferSize];
			using (var stream = new MemoryStream(buffer, 0, buffer.Length, true, true))
			using (var writer = new BinaryWriter(stream, Encoding.UTF8))
			{
				writer.Write(maxMessageSize);
				writer.Write(rpcId);
				writer.Write((byte)MessageType.Call);
				writer.Write(servantId);
				writer.Write(interfaceType);
				writer.Write(methodName);

				if (arguments != null)
				{
					byte[] data = arguments.GetBuffer();
					var dataLength = (int)arguments.Length;
					writer.Write(data, 0, dataLength);
				}

				writer.Flush();

				bufferSize = (int)stream.Position;
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

		protected bool ReadWelcomeMessage(Socket socket, TimeSpan remaining, EndPoint ep)
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

		[Flags]
		private enum MessageType : byte
		{
			Call = 0x1,
			Return = 0x2,
			Exception = 0x4,
		}

		public override string ToString()
		{
			return _name;
		}
	}
}