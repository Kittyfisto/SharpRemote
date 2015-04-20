using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using SharpRemote.CodeGeneration;

namespace SharpRemote
{
	public sealed class PeerEndPoint
		: IEndPoint
		, IEndPointChannel
	{
		private readonly CancellationTokenSource _cancel;
		private readonly NetPeerConfiguration _configuration;
		private readonly Dictionary<IPEndPoint, NetConnection> _connections;
		private readonly Dictionary<ulong, IServant> _servants;
		private readonly NetPeer _peer;
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;
		private readonly Task _task;
		private IPEndPoint _localAddress;
		private ulong _nextMessageId;
		private readonly Dictionary<ulong, Action<NetIncomingMessage>> _pending;

		public PeerEndPoint(string appName, IPAddress localAddress)
		{
			if (appName == null) throw new ArgumentNullException("appName");
			if (localAddress == null) throw new ArgumentNullException("localAddress");

			_servantCreator = new ServantCreator();

			_configuration = new NetPeerConfiguration(appName)
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
			_connections = new Dictionary<IPEndPoint, NetConnection>();
			_servants = new Dictionary<ulong, IServant>();
			_proxyCreator = new ProxyCreator(this);
			_pending = new Dictionary<ulong, Action<NetIncomingMessage>>();
		}

		public IPEndPoint Address
		{
			get { return _localAddress; }
		}

		public void Dispose()
		{
		}

		private void ReceiveMessage()
		{
			CancellationToken token = _cancel.Token;
			while (!token.IsCancellationRequested)
			{
				NetIncomingMessage msg;
				if ((msg = _peer.ReadMessage()) != null)
				{
					switch (msg.MessageType)
					{
						case NetIncomingMessageType.StatusChanged:
							Console.WriteLine("Status: {0}", msg.ReadString());
							break;

						case NetIncomingMessageType.DebugMessage:
							Console.WriteLine("Debug: {0}", msg.ReadString());
							break;

						case NetIncomingMessageType.VerboseDebugMessage:
							Console.WriteLine("Debug: {0}", msg.ReadString());
							break;

						case NetIncomingMessageType.WarningMessage:
							Console.WriteLine("Warning: {0}", msg.ReadString());
							break;

						case NetIncomingMessageType.Error:
							Console.WriteLine("Error: {0}", msg.ReadString());
							break;

						case NetIncomingMessageType.ErrorMessage:
							Console.WriteLine("Error Message: {0}", msg.ReadString());
							break;

						case NetIncomingMessageType.ConnectionApproval:
							Console.WriteLine("Incoming connection from '{0}', approving it...", msg.SenderEndPoint);
							msg.SenderConnection.Approve();
							_connections.Add(msg.SenderEndPoint, msg.SenderConnection);
							break;
					}
				}
				else
				{
					if (token.WaitHandle.WaitOne(1))
						break;
				}
			}
		}

		public void Start()
		{
			_peer.Start();
			_task.Start();
			_localAddress = new IPEndPoint(_configuration.LocalAddress, _peer.Port);
		}

		/// <summary>
		///     Creates a new object of type T.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		public T CreateProxy<T>(ulong objectId) where T : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <param name="subject"></param>
		/// <returns></returns>
		public IServant CreateServant<T>(ulong objectId, T subject) where T : class
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="subject"></param>
		/// <returns></returns>
		public IServant CreateServant<T>(T subject) where T : class
		{
			return _servantCreator.CreateServant(1, subject);
		}

		public void Connect(IPEndPoint address)
		{
			NetConnection connection = _peer.Connect(address);
			_connections.Add(address, connection);
			Thread.Sleep(TimeSpan.FromMinutes(10));
		}

		MemoryStream IEndPointChannel.CallRemoteMethod(ulong objectId, string methodName, MemoryStream arguments)
		{
			var con = _connections.First().Value;
			var msg = _peer.CreateMessage();
			var id = ++_nextMessageId;
			msg.Write(id);
			msg.Write(objectId);
			msg.Write(methodName);
			msg.Write(arguments.GetBuffer(), 0, (int) arguments.Length);
			NetIncomingMessage message;
			SendAndWaitFor(con, id, msg, out message);

			var data = message.Data;
			return new MemoryStream(data, 8, data.Length - 8);
		}

		private void SendAndWaitFor(NetConnection con, ulong id, NetOutgoingMessage msg, out NetIncomingMessage netOutgoingMessage)
		{
			var handle = new ManualResetEvent(false);
			try
			{
				NetIncomingMessage receivedMessage = null;
				_pending.Add(id, message =>
					{
						var actualId = message.PeekUInt64();
						if (actualId == id)
						{
							receivedMessage = message;
							handle.Set();
						}
					});
				var result = con.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, 0);
				if (handle.WaitOne(TimeSpan.FromSeconds(60)))
					throw new TimeoutException();

				netOutgoingMessage = receivedMessage;
			}
			finally
			{
				handle.Dispose();
				_pending.Remove(id);
			}
		}
	}
}