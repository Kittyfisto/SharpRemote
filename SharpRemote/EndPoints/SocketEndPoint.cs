using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using SharpRemote.CodeGeneration;

namespace SharpRemote.EndPoints
{
	public sealed class SocketEndPoint
		: IRemotingEndPoint
		, IEndPointChannel
	{
		private readonly IPEndPoint _localEndPoint;
		private readonly string _name;
		private readonly Socket _serverSocket;
		private readonly Dictionary<ulong, IProxy> _proxies;
		private readonly Dictionary<ulong, IServant> _servants;
		private readonly ProxyCreator _proxyCreator;
		private readonly ServantCreator _servantCreator;
		private IPEndPoint _remoteEndPoint;
		private Socket _socket;

		public SocketEndPoint(IPAddress localAddress, string name)
		{
			if (localAddress == null) throw new ArgumentNullException("localAddress");

			_serverSocket = CreateSocketAndBindToAnyPort(localAddress, out _localEndPoint);
			_serverSocket.Listen(1);
			_serverSocket.BeginAccept(OnIncomingConnection, null);

			_servants = new Dictionary<ulong, IServant>();
			_proxies = new Dictionary<ulong, IProxy>();

			_servantCreator = new ServantCreator(this);
			_proxyCreator = new ProxyCreator(this);

			_name = name;
		}

		private void OnIncomingConnection(IAsyncResult ar)
		{
			try
			{
				_socket = _serverSocket.EndAccept(ar);
				_remoteEndPoint = (IPEndPoint) _socket.RemoteEndPoint;
			}
			catch (Exception)
			{
				
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
				using (var handle = new ManualResetEvent(false))
				{
					socket.BeginConnect(endPoint, ar =>
					{
						try
						{
							socket.EndConnect(ar);
						}
						catch (Exception)
						{ }

						try
						{
							handle.Set();
						}
						catch (Exception)
						{}

					}, null);

					if (!handle.WaitOne(timeout))
						throw new NoSuchEndPointException(endPoint);

					_socket = socket;
					_remoteEndPoint = endPoint;
				}
			}
			catch (Exception)
			{
				socket.Dispose();
				throw;
			}
		}

		public void Disconnect()
		{
			if (_socket != null)
			{
				_socket.Disconnect(false);
				_socket.Dispose();
				_socket = null;
			}

			_remoteEndPoint = null;
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

			throw new NotImplementedException();
		}
	}
}