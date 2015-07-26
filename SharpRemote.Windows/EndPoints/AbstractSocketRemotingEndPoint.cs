using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.CodeGeneration.Remoting;
using SharpRemote.Exceptions;
using SharpRemote.Tasks;
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

		private const string AuthenticationRequiredMessage = "Authentication required";
		private const string NoAuthenticationRequiredMessage = "No Authentication required";
		private const string AuthenticationResponseMessage = "Authentication";
		private const string AuthenticationFailedMessage = "Authentication failed";
		private const string AuthenticationSucceedMessage = "Authentication succeeded";
		protected const string HandshakeSucceedMessage = "Handshake succeeded";

		#region Authentication

		private readonly IAuthenticator _clientAuthenticator;
		private readonly IAuthenticator _serverAuthenticator;

		#endregion

		#region Code Generation

		private readonly AssemblyBuilder _assembly;
		private readonly ModuleBuilder _module;
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;

		#endregion

		private readonly object _syncRoot;

		#region Proxies / Servants

		private readonly Dictionary<ulong, IProxy> _proxiesById;
		private readonly Dictionary<ulong, IServant> _servantsById;
		private readonly Dictionary<object, IServant> _servantsBySubject;

		#endregion

		#region Method Invocation

		private readonly PendingMethodsQueue _pendingMethodCalls;
		private readonly HashSet<MethodInvocation> _pendingMethodInvocations;
		protected CancellationTokenSource _cancellationTokenSource;

		#endregion

		private readonly Serializer _serializer;
		private bool _isDisposed;
		private long _nextRpcId;
		private Socket _socket;
		private readonly string _name;
		private EndPointDisconnectReason? _disconnectReason;

		#region Statistics

		private long _numBytesSent;
		private long _numBytesReceived;
		private long _numCallsInvoked;
		private long _numCallsAnswered;
		private readonly GrainIdGenerator _idGenerator;

		/// <summary>
		/// The total amount of bytes that have been sent over the underlying socket.
		/// </summary>
		public long NumBytesSent
		{
			get { return Interlocked.Read(ref _numBytesSent); }
		}

		/// <summary>
		/// The total amount of bytes that have been received over the underlying socket.
		/// </summary>
		public long NumBytesReceived
		{
			get { return Interlocked.Read(ref _numBytesReceived); }
		}

		/// <summary>
		/// The total amount of remote procedure calls that have been invoked from this end.
		/// </summary>
		public long NumCallsInvoked
		{
			get { return Interlocked.Read(ref _numCallsInvoked); }
		}

		/// <summary>
		/// The total amount of remote procedure calls that have been invoked from the other end.
		/// </summary>
		public long NumCallsAnswered
		{
			get { return Interlocked.Read(ref _numCallsAnswered); }
		}

		#endregion

		protected abstract EndPoint InternalLocalEndPoint { get; }
		protected abstract EndPoint InternalRemoteEndPoint { get; set; }
		protected object SyncRoot{get { return _syncRoot; }}
		protected Socket Socket { get { return _socket; } set { _socket = value; } }

		protected static bool IsFailure(EndPointDisconnectReason reason)
		{
			switch (reason)
			{
				case EndPointDisconnectReason.RequestedByEndPoint:
				case EndPointDisconnectReason.RequestedByRemotEndPoint:
					return false;

				default:
					return true;
			}
		}

		protected sealed class ThreadArgs
		{
			public readonly Socket Socket;
			public readonly CancellationToken Token;

			public ThreadArgs(Socket socket, CancellationToken token)
			{
				Socket = socket;
				Token = token;
			}
		}

		internal AbstractSocketRemotingEndPoint(GrainIdGenerator idGenerator,
			string name,
		                                         IAuthenticator clientAuthenticator = null,
		                                         IAuthenticator serverAuthenticator = null,
		                                         ITypeResolver customTypeResolver = null)
		{
			if (idGenerator == null) throw new ArgumentNullException("idGenerator");

			_idGenerator = idGenerator;
			_name = name ?? "<Unnamed>";
			_syncRoot = new object();

			_servantsById = new Dictionary<ulong, IServant>();
			_servantsBySubject = new Dictionary<object, IServant>();

			_proxiesById = new Dictionary<ulong, IProxy>();

			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			string moduleName = assemblyName.Name + ".dll";
			_module = _assembly.DefineDynamicModule(moduleName);

			_serializer = new Serializer(_module, customTypeResolver);
			_servantCreator = new ServantCreator(_module, _serializer, this, this);
			_proxyCreator = new ProxyCreator(_module, _serializer, this, this);
			_pendingMethodCalls = new PendingMethodsQueue();
			_pendingMethodInvocations = new HashSet<MethodInvocation>();

			_clientAuthenticator = clientAuthenticator;
			_serverAuthenticator = serverAuthenticator;
		}

		#region Reading from / Writing to socket

		protected void WriteLoop(object sock)
		{
			var args = (ThreadArgs) sock;
			var socket = args.Socket;
			var token = args.Token;

			EndPointDisconnectReason reason;

			try
			{
				while (true)
				{
					if (token.IsCancellationRequested)
					{
						reason = EndPointDisconnectReason.RequestedByEndPoint;
						break;
					}

					int messageLength;
					var message = _pendingMethodCalls.TakePendingWrite(token, out messageLength);

					if (message == null)
					{
						reason = EndPointDisconnectReason.RequestedByEndPoint;
						break;
					}

					SocketError error;
					if (!SynchronizedWrite(socket, message, messageLength, out error))
					{
						reason = EndPointDisconnectReason.WriteFailure;
						break;
					}
				}

			}
			catch (OperationCanceledException)
			{
				reason = EndPointDisconnectReason.RequestedByEndPoint;
			}
			catch (Exception e)
			{
				reason = EndPointDisconnectReason.UnhandledException;
				Log.ErrorFormat("Caught exception while writing/handling messages: {0}", e);
			}

			Disconnect(reason);
		}

		protected void ReadLoop(object sock)
		{
			var args = (ThreadArgs)sock;
			var socket = args.Socket;

			EndPointDisconnectReason reason;

			try
			{
				var size = new byte[4];
				while (true)
				{
					SocketError err;
					if (!SynchronizedRead(socket, size, out err))
					{
						reason = EndPointDisconnectReason.ReadFailure;
						break;
					}

					int length = BitConverter.ToInt32(size, 0);
					if (length >= 8)
					{
						var buffer = new byte[length];
						if (!SynchronizedRead(socket, buffer, out err))
						{
							reason = EndPointDisconnectReason.ReadFailure;
							break;
						}

						var stream = new MemoryStream(buffer, false);
						var reader = new BinaryReader(stream);
						long rpcId = reader.ReadInt64();
						var type = (MessageType)reader.ReadByte();

						Interlocked.Add(ref _numBytesReceived, length + 4);

						EndPointDisconnectReason? r;
						if (!HandleMessage(rpcId, type, reader, out r))
						{
// ReSharper disable PossibleInvalidOperationException
							reason = (EndPointDisconnectReason) r;
// ReSharper restore PossibleInvalidOperationException

							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				reason = EndPointDisconnectReason.UnhandledException;
				Log.ErrorFormat("Caught exception while reading/handling messages: {0}", e);
			}

			Disconnect(reason);
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
					Log.DebugFormat("Error while reading from socket: {0} out of {1} read, method {2}, IsConnected: {3}", read,
									buffer.Length, err, socket.Connected);
					return false;
				}
			}

			return true;
		}

		#endregion

		#region Timers

		//private readonly Stopwatch _createMessage = new Stopwatch();

		#endregion

		public MemoryStream CallRemoteMethod(ulong servantId, string interfaceType, string methodName, MemoryStream arguments)
		{
			Socket socket = _socket;
			//if (socket == null || !socket.Connected)
			if (socket == null)
				throw new NotConnectedException(_name);

			long rpcId = Interlocked.Increment(ref _nextRpcId);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("{0} to {1}: sending RPC #{2} to {3}.{4}",
								InternalLocalEndPoint,
								InternalRemoteEndPoint,
								rpcId,
								servantId,
								methodName);
			}

			PendingMethodCall call = null;
			try
			{
				//_createMessage.Start();
				call = _pendingMethodCalls.Enqueue(servantId,
				                                   interfaceType,
				                                   methodName,
				                                   arguments,
												   rpcId);
				//_createMessage.Stop();

				Interlocked.Add(ref _numBytesSent, call.MessageLength);
				Interlocked.Increment(ref _numCallsInvoked);

				call.Wait();

				if (call.MessageType == MessageType.Return)
				{
					return (MemoryStream)call.Reader.BaseStream;
				}
				else if ((call.MessageType & MessageType.Exception) != 0)
				{
					var formatter = new BinaryFormatter();
					var e = (Exception)formatter.Deserialize(call.Reader.BaseStream);
					throw e;
				}
				else
				{
					throw new NotImplementedException();
				}
			}
			finally
			{
				if (call != null)
				{
					_pendingMethodCalls.Recycle(call);
				}
			}
		}

		/// <summary>
		/// Tests if this object has been disposed of or not.
		/// </summary>
		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				//_pendingWrites.Dispose();

				Disconnect();
				DisposeAdditional();

				//Console.WriteLine("Create Message: {0:D}ms", _createMessage.ElapsedMilliseconds);

				_isDisposed = true;
			}
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

		private void Disconnect(EndPointDisconnectReason reason)
		{
			lock (_syncRoot)
			{
				if (_socket != null)
				{
					_disconnectReason = reason;
					if (IsFailure(reason))
					{
						var fn = OnFailure;
						if (fn != null)
						{
							try
							{
								fn(reason);
							}
							catch (Exception e)
							{
								Log.WarnFormat("The OnFailure event threw an exception, please don't do that: {0}", e);
							}
						}
					}

					Log.InfoFormat("Disconnecting socket '{0}' from {1}: {2}", _name, InternalRemoteEndPoint, reason);

					_cancellationTokenSource.Cancel();
					_pendingMethodCalls.CancelAllCalls();

					// If we are disconnecting because of a failure, then we don't notify the other end
					// and drop the connection immediately. Also there's no need to notify the other
					// end when it requested the disconnect
					if (!IsFailure(reason) && reason != EndPointDisconnectReason.RequestedByRemotEndPoint)
					{
						SendGoodbye();
					}

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

		/// <summary>
		/// Contains the reason why the socket was disconnected, or null if it wasn't disconnected / never established
		/// a connection.
		/// </summary>
		public EndPointDisconnectReason? DisconnectReason
		{
			get { return _disconnectReason; }
		}

		private void SendGoodbye()
		{
			try
			{
				var rpcId = _nextRpcId++;
				const int messageSize = 9;

				using (var stream = new MemoryStream())
				using (var writer = new BinaryWriter(stream, Encoding.UTF8))
				{
					writer.Write(messageSize);
					writer.Write(rpcId);
					writer.Write((byte)MessageType.Goodbye);

					writer.Flush();
					stream.Position = 0;

					_socket.Send(stream.GetBuffer(), 0, messageSize + 4, SocketFlags.None);
				}
			}
			catch (SocketException)
			{

			}
		}

		/// <summary>
		/// This event is invoked right before a socket is to be closed due to failure of:
		/// - the connection between endpoints
		/// - a failure of the remote process
		/// - a failure of SharpRemote
		/// - something else ;)
		/// </summary>
		public event Action<EndPointDisconnectReason> OnFailure;

		public void Disconnect()
		{
			Disconnect(EndPointDisconnectReason.RequestedByEndPoint);
		}

		public T CreateProxy<T>(ulong objectId) where T : class
		{
			lock (_proxiesById)
			{
				var proxy = _proxyCreator.CreateProxy<T>(objectId);
				_proxiesById.Add(objectId, (IProxy)proxy);
				return proxy;
			}
		}

		/// <summary>
		/// Returns all the proxies of this endpoint.
		/// Used for testing.
		/// </summary>
		internal IEnumerable<IProxy> Proxies
		{
			get
			{
				lock (_proxiesById)
				{
					return _proxiesById.Values.ToList();
				}
			}
		}

		/// <summary>
		/// Returns all the servnats of this endpoint.
		/// Used for testing.
		/// </summary>
		internal IEnumerable<IServant> Servants
		{
			get
			{
				lock (_servantsById)
				{
					return _servantsById.Values.ToList();
				}
			}
		}

		public T GetProxy<T>(ulong objectId) where T : class
		{
			IProxy proxy;
			if (!_proxiesById.TryGetValue(objectId, out proxy))
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
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Creating new servant (#{2}) '{0}' implementing '{1}'",
								subject.GetType().FullName,
				                typeof (T).FullName,
				                objectId
					);
			}

			IServant servant = _servantCreator.CreateServant(objectId, subject);
			lock (_servantsById)
			{
				_servantsById.Add(objectId, servant);
				_servantsBySubject.Add(subject, servant);
			}
			return servant;
		}

		public T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class
		{
			lock (_proxiesById)
			{
				IProxy proxy;
				if (!_proxiesById.TryGetValue(objectId, out proxy))
				{
					return CreateProxy<T>(objectId);
				}

				return (T) proxy;
			}
		}

		public IServant GetExistingOrCreateNewServant<T>(T subject) where T : class
		{
			lock (_servantsById)
			{
				IServant servant;
				if (!_servantsBySubject.TryGetValue(subject, out servant))
				{
					var nextId = _idGenerator.GetGrainId();
					servant = CreateServant(nextId, subject);
				}

				return servant;
			}
		}

		private bool HandleMessage(long rpcId, MessageType type, BinaryReader reader, out EndPointDisconnectReason? reason)
		{
			if (type == MessageType.Call)
			{
				Interlocked.Increment(ref _numCallsAnswered);
				HandleRequest(rpcId, reader);
			}
			else if ((type & MessageType.Return) != 0)
			{
				if (!HandleResponse(rpcId, type, reader))
				{
					Log.ErrorFormat("There is no pending RPC of id '{0}' - disconnecting...", rpcId);
					reason = EndPointDisconnectReason.RpcInvalidResponse;
					return false;
				}
			}
			else if ((type & MessageType.Goodbye) != 0)
			{
				Log.InfoFormat("Connection about to be closed by the other side - disconnecting...");

				reason = EndPointDisconnectReason.RequestedByRemotEndPoint;
				return false;
			}
			else
			{
				throw new NotImplementedException();
			}

			reason = null;
			return true;
		}

		private void DispatchMethodInvocation(long rpcId, IGrain grain, string typeName, string methodName, BinaryReader reader)
		{
			// There's 2 things we can find and report immediately:
			// 1. an invalid object-id was passed that points to a different interface than the caller expects
			// 2. no task scheduler could be found for the required method.
			try
			{
				EnsureTypeSafety(grain.ObjectId, grain.InterfaceType, typeName, methodName);
				TaskScheduler taskScheduler = grain.GetTaskScheduler(methodName);

				// However if those 2 things don't throw, then we dispatch the rest of the method invocation
				// on the task dispatcher and be done with it here...
				var task = new Task((object accessToken) =>
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
							if (Log.IsErrorEnabled)
							{
								Log.ErrorFormat("Caught exception while executing RPC #{0} on {1}.{2} (#{3}): {4}",
								                rpcId,
								                typeName,
								                methodName,
								                grain.ObjectId,
								                e);
							}

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
				}, SerialTaskScheduler.AccessToken); //< We need to specify an access token so this task gets scheduled in a serial manner

				// Once we've created the task, we remember that there's a method invocation
				// that's yet to be executed (which tremendously helps debugging problems)
				var methodInvocation = new MethodInvocation(rpcId, grain, methodName, task);
				task.ContinueWith(unused =>
				{
					lock (_pendingMethodInvocations)
					{
						_pendingMethodInvocations.Remove(methodInvocation);
					}
				});

				lock (_pendingMethodInvocations)
				{
					_pendingMethodInvocations.Add(methodInvocation);
				}

				// And then finally start the task to deserialize all method parameters, invoke the mehtod
				// and then seralize either the return value of the thrown exception...
				task.Start(taskScheduler);
			}
			catch (TypeMismatchException e)
			{
				var response = new MemoryStream();
				var writer = new BinaryWriter(response, Encoding.UTF8);
				response.Position = 0;
				WriteResponseHeader(rpcId, writer, MessageType.Return | MessageType.Exception);
				WriteException(writer, e);
				PatchResponseMessageLength(response, writer);

				var responseLength = (int)response.Length;
				byte[] data = response.GetBuffer();

				SocketError err;
				Socket socket = _socket;
				if (!SynchronizedWrite(socket, data, responseLength, out err))
				{
					Log.ErrorFormat("Disconnecting socket due to error while writing response!");
					Disconnect();
				}
			}
		}

		private void HandleRequest(long rpcId, BinaryReader reader)
		{
			ulong servantId = reader.ReadUInt64();
			string typeName = reader.ReadString();
			string methodName = reader.ReadString();

			IServant servant;
			lock (_servantsById)
			{
				_servantsById.TryGetValue(servantId, out servant);
			}

			if (servant != null)
			{
				DispatchMethodInvocation(rpcId, servant, typeName, methodName, reader);
			}
			else
			{
				IProxy proxy;
				lock (_proxiesById)
				{
					_proxiesById.TryGetValue(servantId, out proxy);
				}

				if (proxy != null)
				{
					DispatchMethodInvocation(rpcId, proxy, typeName, methodName, reader);
				}
				else
				{
					throw new NoSuchServantException(servantId);
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
			return _pendingMethodCalls.HandleResponse(rpcId, messageType, reader);
		}

		protected void ReadMessage(Socket socket, TimeSpan timeout, out string messageType, out string message)
		{
			var remoteEndPoint = socket.RemoteEndPoint;
			var size = new byte[4];
			SocketError err;
			if (!SynchronizedRead(socket, size, timeout, out err))
			{
				throw new HandshakeException(string.Format("Failed to receive message from endpoint '{0}' in time: {1}s (error: {2})", remoteEndPoint, timeout.TotalSeconds, err));
			}

			int length = BitConverter.ToInt32(size, 0);
			if (length < 0)
			{
				throw new HandshakeException(string.Format("The message received from endpoint '{0}' is malformatted", remoteEndPoint));
			}

			var buffer = new byte[length];
			if (!SynchronizedRead(socket, buffer, timeout, out err))
			{
				throw new HandshakeException(string.Format("Failed to receive message from endpoint '{0}' in time: {1}s (error: {2})", remoteEndPoint, timeout.TotalSeconds, err));
			}

			using (var reader = new BinaryReader(new MemoryStream(buffer)))
			{
				messageType = reader.ReadString();
				message = reader.ReadString();
			}
		}

		protected void WriteMessage(Socket socket,
			string messageType,
			string message = "")
		{
			var remoteEndPoint = socket.RemoteEndPoint;
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				stream.Position = 4;
				writer.Write(messageType);
				writer.Write(message);
				writer.Flush();
				PatchResponseMessageLength(stream, writer);
				stream.Position = 0;

				SocketError err;
				if (!SynchronizedWrite(socket, stream.GetBuffer(), (int) stream.Length, out err))
				{
					throw new HandshakeException(string.Format("Failed to send {0} to endpoint '{1}': {2}",
						messageType,
						remoteEndPoint,
						err));
				}
			}
		}

		/// <summary>
		/// Performs the authentication between client & server (if necessary) from the server-side.
		/// </summary>
		/// <param name="socket"></param>
		protected void PerformIncomingHandshake(Socket socket)
		{
			EndPoint remoteEndPoint = socket.RemoteEndPoint;
			var timeout = TimeSpan.FromMinutes(1);
			string messageType;
			string message;

			if (_clientAuthenticator != null)
			{
				// Upon accepting an incoming connection, we try to authenticate the client
				// by posing a challenge
				string challenge = _clientAuthenticator.CreateChallenge();
				Log.DebugFormat("Creating challenge '{0}' for endpoint '{1}'", challenge, remoteEndPoint);
				WriteMessage(socket, AuthenticationRequiredMessage, challenge);

				ReadMessage(socket, timeout, out messageType, out message);
				Log.DebugFormat("Received response '{0}' for challenge '{1}' from endpoint '{2}'",
				                message,
				                challenge,
				                remoteEndPoint);

				if (!_clientAuthenticator.Authenticate(challenge, message))
				{
					// Should the client fail the challenge, we tell him that,
					// but drop the connection immediately afterwards.
					WriteMessage(socket, AuthenticationFailedMessage);
					throw new AuthenticationException(string.Format("Endpoint '{0}' failed the authentication challenge",
					                                                remoteEndPoint));
				}

				WriteMessage(socket, AuthenticationSucceedMessage);
				Log.InfoFormat("Endpoint '{0}' successfully authenticated", remoteEndPoint);
			}
			else
			{
				WriteMessage(socket, NoAuthenticationRequiredMessage);
			}

			ReadMessage(socket, timeout, out messageType, out message);
			if (messageType == AuthenticationRequiredMessage)
			{
				if (_serverAuthenticator == null)
					throw new AuthenticationRequiredException(string.Format("Endpoint '{0}' requires authentication", remoteEndPoint));

				string challenge = message;
				string response = _serverAuthenticator.CreateResponse(challenge);
				WriteMessage(socket, AuthenticationResponseMessage, response);

				// After having answered the challenge we wait for a successful response from the client.
				// If we failed the authentication, then 
				ReadMessage(socket, timeout, out messageType, out message);
				if (messageType != AuthenticationSucceedMessage)
					throw new AuthenticationException(string.Format("Failed to authenticate against endpoint '{0}'", remoteEndPoint));
			}
			else if (messageType != NoAuthenticationRequiredMessage)
			{
				throw new HandshakeException();
			}

			OnHandshakeSucceeded(socket);
			WriteMessage(socket, HandshakeSucceedMessage);
		}

		/// <summary>
		/// Performs the authentication between client & server (if necessary) from the client-side.
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="timeout"></param>
		protected void PerformOutgoingHandshake(Socket socket, TimeSpan timeout)
		{
			string messageType;
			string message;
			EndPoint remoteEndPoint = socket.RemoteEndPoint;

			ReadMessage(socket, timeout, out messageType, out message);
			if (messageType == AuthenticationRequiredMessage)
			{
				if (_clientAuthenticator == null)
					throw new AuthenticationRequiredException(string.Format("Endpoint '{0}' requires authentication", remoteEndPoint));

				string challenge = message;
				// Upon establishing a connection, we try to authenticate the ourselves
				// against the server by answering his response.
				string response = _clientAuthenticator.CreateResponse(challenge);
				WriteMessage(socket, AuthenticationResponseMessage, response);

				// If we failed the authentication, a proper server will tell us so we can
				// forward this information to the caller.
				ReadMessage(socket, timeout, out messageType, out message);
				if (messageType != AuthenticationSucceedMessage)
					throw new AuthenticationException(string.Format("Failed to authenticate against endpoint '{0}'", remoteEndPoint));
			}
			else if (messageType != NoAuthenticationRequiredMessage)
			{
				throw new HandshakeException(string.Format("EndPoint '{0}' sent unknown message '{1}: {2}', expected either {3} or {4}",
					remoteEndPoint,
					messageType,
					message,
					AuthenticationRequiredMessage,
					NoAuthenticationRequiredMessage
					));
			}

			if (_serverAuthenticator != null)
			{
				// After we've authenticated ourselves, it's time for the server to authenticate himself.
				// Let's send the challenge
				string challenge = _serverAuthenticator.CreateChallenge();
				WriteMessage(socket, AuthenticationRequiredMessage, challenge);
				ReadMessage(socket, timeout, out messageType, out message);
				if (!_serverAuthenticator.Authenticate(challenge, message))
				{
					// Should the server fail to authenticate himself, then we tell him that end then abort
					// the connection...
					WriteMessage(socket, AuthenticationResponseMessage, AuthenticationFailedMessage);
					throw new AuthenticationException(string.Format("Endpoint '{0}' failed the authentication challenge",
					                                                remoteEndPoint));
				}

				WriteMessage(socket, AuthenticationSucceedMessage);
				Log.InfoFormat("Endpoint '{0}' successfully authenticated", remoteEndPoint);
			}
			else
			{
				WriteMessage(socket, NoAuthenticationRequiredMessage);
			}

			ReadMessage(socket, timeout, out messageType, out message);
			if (messageType != HandshakeSucceedMessage)
				throw new HandshakeException(string.Format("Endpoint '{0}' failed to finished the handshake", remoteEndPoint));

			OnHandshakeSucceeded(socket);
		}

		/// <summary>
		/// Is called when the handshake for the newly incoming message succeeds.
		/// </summary>
		/// <param name="socket"></param>
		protected abstract void OnHandshakeSucceeded(Socket socket);

		public override string ToString()
		{
			return _name;
		}
	}
}