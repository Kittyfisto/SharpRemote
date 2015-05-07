using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using SharpRemote.CodeGeneration;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// No longer maintained, DO NOT USE
	/// </summary>
	public sealed class LidgrenEndPoint
		: IRemotingEndPoint
		  , IEndPointChannel
	{
		#region Tokens

		private const string RequestToken = "Request";

		private const string ResponseToken = "Response";

		private const string ResponseSuccessToken = "Success";

		private const string ResponseExceptionToken = "Exception";

		#endregion

		private readonly CancellationTokenSource _cancel;
		private readonly NetPeerConfiguration _configuration;
		private readonly string _name;
		private readonly IPEndPoint _localEndPoint;
		private readonly NetPeer _peer;
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;
		private readonly Dictionary<ulong, IProxy> _proxies;
		private readonly Dictionary<ulong, IServant> _servants;
		private readonly Task _task;
		private long _nextRpcId;

		#region Connection

		private NetConnection _connection;
		private IPEndPoint _remoteEndPoint;

		#endregion

		#region Pending Methods

		private readonly Dictionary<long, Action<string, MemoryStream>> _pendingCalls;
		private readonly Dictionary<IPEndPoint, Action<NetIncomingMessage>> _pendingConnects;

		#endregion

		/// <summary>
		/// Creates a new endpoint on the given address, a randomly chosen but available port and with the given name.
		/// </summary>
		/// <param name="localAddress"></param>
		/// <param name="endPointName">The name of this endpoint, used only for logging to increase readability</param>
		public LidgrenEndPoint(IPAddress localAddress, string endPointName = null)
		{
			if (localAddress == null) throw new ArgumentNullException("localAddress");

			_name = endPointName ?? "<Unnamed>";
			_configuration = new NetPeerConfiguration("Test")
				{
					LocalAddress = localAddress
				};
			_configuration.EnableMessageType(NetIncomingMessageType.Error);
			_configuration.EnableMessageType(NetIncomingMessageType.StatusChanged);
			_configuration.EnableMessageType(NetIncomingMessageType.UnconnectedData);
			_configuration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);
			_configuration.EnableMessageType(NetIncomingMessageType.Data);
			_configuration.EnableMessageType(NetIncomingMessageType.Receipt);
			_configuration.EnableMessageType(NetIncomingMessageType.DiscoveryRequest);
			_configuration.EnableMessageType(NetIncomingMessageType.DiscoveryResponse);
			_configuration.EnableMessageType(NetIncomingMessageType.VerboseDebugMessage);
			_configuration.EnableMessageType(NetIncomingMessageType.DebugMessage);
			_configuration.EnableMessageType(NetIncomingMessageType.WarningMessage);
			_configuration.EnableMessageType(NetIncomingMessageType.ErrorMessage);
			_configuration.EnableMessageType(NetIncomingMessageType.NatIntroductionSuccess);
			_configuration.EnableMessageType(NetIncomingMessageType.ConnectionLatencyUpdated);
			_configuration.AcceptIncomingConnections = true;

			_peer = new NetPeer(_configuration);
			_task = new Task(ReceiveMessage, TaskCreationOptions.LongRunning);

			_cancel = new CancellationTokenSource();

			_servants = new Dictionary<ulong, IServant>();
			_proxies = new Dictionary<ulong, IProxy>();

			_servantCreator = new ServantCreator(this);
			_proxyCreator = new ProxyCreator(this);

			_pendingCalls = new Dictionary<long, Action<string, MemoryStream>>();
			_pendingConnects = new Dictionary<IPEndPoint, Action<NetIncomingMessage>>();

			_peer.Start();
			_task.Start();
			_localEndPoint = new IPEndPoint(_configuration.LocalAddress, _peer.Port);
		}

		#region IRemotingEndPoint interface

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

			var handle = new ManualResetEvent(false);
			try
			{
				lock (_pendingConnects)
				{
					_pendingConnects.Add(endPoint, msg => handle.Set());
				}

				var tmpConnection = _peer.Connect(endPoint);

				if (!handle.WaitOne(timeout))
					throw new NoSuchEndPointException(endPoint);

				_connection = tmpConnection;
				_remoteEndPoint = endPoint;
			}
			finally
			{
				handle.Dispose();

				lock (_pendingConnects)
				{
					_pendingConnects.Remove(endPoint);
				}
			}
		}

		public void Disconnect()
		{
			var con = _connection;
			if (con == null)
				return;

			_connection = null;
			_remoteEndPoint = null;

			con.Disconnect("Screw you, I'll build my own connection, with black jack & hookers");
			InterruptOngoingCalls();
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

		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			IServant servant = _servantCreator.CreateServant(objectId, subject);
			lock (_servants)
			{
				_servants.Add(objectId, servant);
			}
			return servant;
		}

		public void Dispose()
		{
		}

		#endregion

		MemoryStream IEndPointChannel.CallRemoteMethod(ulong servantId, string methodName, MemoryStream arguments)
		{
			NetConnection con = _connection;
			if (con == null)
				throw new NotConnectedException(_name);

			NetOutgoingMessage msg = _peer.CreateMessage();
			long rpcId = Interlocked.Increment(ref _nextRpcId);
			msg.Write(RequestToken);
			msg.Write(rpcId);
			msg.Write(servantId);
			msg.Write(methodName);
			if (arguments != null)
			{
				var length = (int)arguments.Length;
				msg.Write(length);
				msg.Write(arguments.GetBuffer(), 0, length);
			}
			else
			{
				const int length = 0;
				msg.Write(length);
			}

			MemoryStream message;
			string responseToken;
			SendAndWaitFor(con, rpcId, msg, out responseToken, out message);

			switch (responseToken)
			{
				case ResponseSuccessToken:
					return message;

				case ResponseExceptionToken:
					using (var reader = new BinaryReader(message, Encoding.UTF8))
					{
						var formatter = new BinaryFormatter();
						var e = (Exception)formatter.Deserialize(reader.BaseStream);
						throw e;
					}

				default:
					throw new NotImplementedException(string.Format("Unexpected token: {0}", responseToken));
			}
		}

		private void ReceiveMessage()
		{
			CancellationToken token = _cancel.Token;
			while (!token.IsCancellationRequested)
			{
				NetIncomingMessage msg;
				if ((msg = _peer.ReadMessage()) != null)
				{
					try
					{
						switch (msg.MessageType)
						{
							case NetIncomingMessageType.StatusChanged:
								NetConnection connection = msg.SenderConnection;
								Console.WriteLine("{0}: Status changed to {1}", _name, connection.Status);
								if (connection.Status == NetConnectionStatus.Connected)
								{
									NotifySuccessfulConnection(msg);
								}
								else if (connection.Status == NetConnectionStatus.Disconnected)
								{
									Disconnect();
								}
								break;

							case NetIncomingMessageType.DebugMessage:
								Console.WriteLine("{0}: DEBUG {1}", _name, msg.ReadString());
								break;

							case NetIncomingMessageType.VerboseDebugMessage:
								Console.WriteLine("{0}: DEBUG {1}", _name, msg.ReadString());
								break;

							case NetIncomingMessageType.WarningMessage:
								Console.WriteLine("{0}: WARN {1}", _name, msg.ReadString());
								break;

							case NetIncomingMessageType.Error:
								Console.WriteLine("{0}: ERROR {1}", _name, msg.ReadString());
								break;

							case NetIncomingMessageType.ErrorMessage:
								Console.WriteLine("{0}: ERROR {1}", _name, msg.ReadString());
								break;

							case NetIncomingMessageType.ConnectionApproval:
								Console.WriteLine("{0}: Incoming connection from '{1}', approving it...", _name, msg.SenderEndPoint);
								msg.SenderConnection.Approve();
								_connection = msg.SenderConnection;
								_remoteEndPoint = msg.SenderEndPoint;
								break;

							case NetIncomingMessageType.Data:
								Console.WriteLine("{0}: Data received from {1}", _name, msg.SenderEndPoint);
								HandleMessage(msg);
								break;
						}
					}
					catch (Exception e)
					{
						Console.WriteLine("{0}: Caught exception while handling msg - {1}", _name, e);
					}
					finally
					{
						_peer.Recycle(msg);
					}
				}
				else
				{
					if (token.WaitHandle.WaitOne(1))
						break;
				}
			}
		}

		private void HandleMessage(NetIncomingMessage msg)
		{
			string type = msg.ReadString();
			long messageId = msg.ReadInt64();

			switch (type)
			{
				case RequestToken:
					HandleRequest(messageId, msg);
					break;

				case ResponseToken:
					HandleResponse(messageId, msg);
					break;

				default:
					// Unhandled
					break;
			}
		}

		/// <summary>
		///     Handles an RPC request (e.g. the call itself) by forwarding the call to the desired <see cref="IServant" />.
		/// </summary>
		/// <param name="rpcId">The ID of the remote procedure call, uniquely identifying this call amonst all other pending ones</param>
		/// <param name="msg">
		///     The incoming message to forward to the <see cref="IServant" />
		/// </param>
		private void HandleRequest(long rpcId, NetIncomingMessage msg)
		{
			ulong id = msg.ReadUInt64();
			string methodName = msg.ReadString();
			int length = msg.ReadInt32();
			var data = new byte[length];
			if (length > 0)
			{
				msg.ReadBytes(data, 0, length);
			}

			Encoding encoding = Encoding.UTF8;

			using (var input = new MemoryStream(data))
			using (var reader = new BinaryReader(input, encoding))
			using (var output = new MemoryStream())
			using (var writer = new BinaryWriter(output, encoding))
			{
				bool success;

				try
				{
					IServant servant;
					IProxy proxy;
					TryGetProxyOrServant(id, out servant, out proxy);

					if (servant != null)
						servant.InvokeMethod(methodName, reader, writer);
					else if (proxy != null)
						proxy.InvokeEvent(methodName, reader, writer);

					success = true;
				}
				catch (Exception e)
				{
					success = false;

					WriteException(writer, e);
				}

				output.Position = 0;
				SendRpcResponse(rpcId, msg.SenderConnection, success, output);
			}
		}

		private bool TryGetProxyOrServant(ulong id, out IServant servant, out IProxy proxy)
		{
			lock (_servants)
			{
				if (_servants.TryGetValue(id, out servant))
				{
					proxy = null;
					return true;
				}
			}

			lock (_proxies)
			{
				if (_proxies.TryGetValue(id, out proxy))
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		///     Sends a response for the given RPC (<paramref name="rpcId" />) to the client that fired it in the first place.
		/// </summary>
		/// <param name="rpcId"></param>
		/// <param name="connection"></param>
		/// <param name="success"></param>
		/// <param name="output"></param>
		private void SendRpcResponse(long rpcId, NetConnection connection, bool success, MemoryStream output)
		{
			NetOutgoingMessage responseMsg = _peer.CreateMessage();
			responseMsg.Write(ResponseToken);
			responseMsg.Write(rpcId);
			responseMsg.Write(success ? ResponseSuccessToken : ResponseExceptionToken);

			var outLength = (int) output.Length;
			if (outLength > 0)
			{
				responseMsg.Write(outLength);
				responseMsg.Write(output.GetBuffer(), 0, outLength);
			}
			else
			{
				responseMsg.Write(outLength);
			}

			connection.SendMessage(responseMsg, NetDeliveryMethod.ReliableOrdered, 0);
		}

		private void HandleResponse(long rpcId, NetIncomingMessage msg)
		{
			Action<string, MemoryStream> fn;
			lock (_pendingCalls)
			{
				if (!_pendingCalls.TryGetValue(rpcId, out fn))
					return;
			}

			string responseToken = msg.ReadString();
			int length = msg.ReadInt32();
			var response = new byte[length];
			if (length > 0)
			{
				msg.ReadBytes(response, 0, length);
				fn(responseToken, new MemoryStream(response));
			}
			else
			{
				fn(responseToken, null);
			}
		}

		private void NotifySuccessfulConnection(NetIncomingMessage msg)
		{
			lock (_pendingConnects)
			{
				Action<NetIncomingMessage> fn;
				if (_pendingConnects.TryGetValue(msg.SenderEndPoint, out fn))
				{
					Console.WriteLine("{0}: Pending connection to '{1}' approved", _name, msg.SenderEndPoint);

					fn(msg);
				}
				else
				{
					// What to do now?!
				}
			}
		}

		private void SendAndWaitFor(NetConnection con, long rpcId, NetOutgoingMessage msg, out string responseToken,
		                            out MemoryStream response)
		{
			var handle = new ManualResetEvent(false);
			try
			{
				MemoryStream receivedData = null;
				string receivedToken = null;

				lock (_pendingCalls)
				{
					_pendingCalls.Add(rpcId, (token, message) =>
						{
							receivedToken = token;
							receivedData = message;
							handle.Set();
						});
				}

				con.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
				handle.WaitOne();

				response = receivedData;
				responseToken = receivedToken;
			}
			finally
			{
				handle.Dispose();
				_pendingCalls.Remove(rpcId);
			}
		}

		private void WriteException(BinaryWriter writer, Exception e)
		{
			Stream stream = writer.BaseStream;
			long start = stream.Position;
			var formatter = new BinaryFormatter();

			try
			{
				formatter.Serialize(stream, e);
			}
			catch (SerializationException)
			{
				// TODO: Log this..
				writer.Flush();
				stream.Position = start;
				formatter.Serialize(stream, new UnserializableException(e));
			}
		}

		private void InterruptOngoingCalls()
		{
			lock (_pendingCalls)
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);
				WriteException(writer, new ConnectionLostException());

				foreach (var call in _pendingCalls.Values)
				{
					stream.Position = 0;
					call(ResponseExceptionToken, stream);
				}
				_pendingCalls.Clear();
			}
		}
	}
}