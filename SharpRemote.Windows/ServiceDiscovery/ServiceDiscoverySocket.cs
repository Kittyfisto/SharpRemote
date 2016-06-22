using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using log4net;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	/// Service discovery socket implementation that is bound to a single address (and thus to a single network interface).
	/// </summary>
	internal sealed class ServiceDiscoverySocket
		: IServiceDiscoverySocket
		, IDisposable
	{
		private readonly INetworkServiceRegisty _services;
		private const int MaxLength = 508;
		private const string NoName = "";

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Socket _socket;
		private readonly IPAddress _localAddress;
		private readonly IPEndPoint _localEndPoint;
		private readonly IPEndPoint _multicastEndPoint;
		private readonly IPAddress _multicastAddress;

		private bool _isDisposed;

		public ServiceDiscoverySocket(NetworkInterface iface,
		                              IPAddress localAddress,
		                              IPAddress multicastAddress,
		                              int port,
		                              int ttl,
		                              INetworkServiceRegisty services)
		{
			_services = services;
			_localAddress = localAddress;
			_localEndPoint = new IPEndPoint(_localAddress, port);
			_multicastAddress = multicastAddress;
			_multicastEndPoint = new IPEndPoint(multicastAddress, port);

			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
				{
					MulticastLoopback = true,
				};
			_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, ttl);
			_socket.SetSocketOption(SocketOptionLevel.Socket,
			                        SocketOptionName.ReuseAddress, true);
			_socket.Bind(new IPEndPoint(localAddress, port));

			var addressFamily = _localAddress.AddressFamily;
			if (addressFamily == AddressFamily.InterNetwork)
			{
				try
				{
					_socket.SetSocketOption(SocketOptionLevel.IP,
					                        SocketOptionName.AddMembership,
					                        new MulticastOption(_multicastAddress,
					                                            _localAddress));

					Log.DebugFormat("Joined multicast group {0} for {1}@{2}",
					                _multicastAddress,
					                _localAddress,
					                iface.Name);
				}
				catch (SocketException e)
				{
					Log.DebugFormat("Unable to join multicast group {0} for {1}@{2}: {3}",
					                _multicastAddress,
					                _localAddress,
					                iface.Name,
					                e);
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException(string.Format("Service discovery is not implemented for '{0}'", addressFamily));
			}

			BeginReceive();
		}

		private void BeginReceive()
		{
			lock (_socket)
			{
				if (_isDisposed)
					return;

				var buffer = new byte[MaxLength];
				EndPoint endPoint = _localEndPoint;
				_socket.BeginReceiveFrom(buffer, 0, buffer.Length,
				                         SocketFlags.None,
				                         ref endPoint,
				                         OnReceived, buffer);
			}
		}

		private void OnReceived(IAsyncResult ar)
		{
			try
			{
				EndPoint remoteEndPoint = _localEndPoint;
				int length;
				var buffer = (byte[])ar.AsyncState;
				lock (_socket)
				{
					if (_isDisposed)
						return;

					length = _socket.EndReceiveFrom(ar, ref remoteEndPoint);
				}

				string token;
				string name;
				IPEndPoint endPoint;
				if (Message.TryRead(buffer, remoteEndPoint, out token, out name, out endPoint))
				{
					switch (token)
					{
						case Message.P2PQueryToken:
							SendResponse(name, remoteEndPoint);
							break;

						case Message.P2PResponseToken:
							Action<Service> fn = OnResponseReceived;
							if (fn != null)
								fn(new Service(name, endPoint, _localAddress));
							break;
					}
				}
				else
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("Received invalid response ({0} bytes) from '{1}', ignoring it",
						                length,
						                remoteEndPoint);
					}
				}
			}
			catch (SocketException e)
			{
				Log.ErrorFormat("Caught socket error: {0}", e);
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception: {0}", e);
			}
			finally
			{
				BeginReceive();
			}
		}

		private void SendResponse(string name, EndPoint remoteEndPoint)
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Received query (name: {0}) from '{1}', answering...",
				                name,
				                remoteEndPoint);
			}

			var services = new List<RegisteredService>();
			lock (_socket)
			{
				services.AddRange(_services.GetServicesByName(name));
			}

			lock (_socket)
			{
				if (_isDisposed)
					return;

				foreach (var service in services)
				{
					var endPoint = service.EndPoint;
					if (Equals(endPoint.Address, IPAddress.Any) ||
					    Equals(endPoint.Address, IPAddress.IPv6Any))
					{
						endPoint = new IPEndPoint(_localAddress, endPoint.Port);
					}

					byte[] buffer = Message.CreateResponse(service.Name, endPoint);
					_socket.SendTo(buffer, _multicastEndPoint);
				}
			}
		}

		public void Query(string serviceName)
		{
			byte[] query = Message.CreateQuery(serviceName ?? NoName);

			lock (_socket)
			{
				if (_isDisposed)
					return;

				_socket.SendTo(query, _multicastEndPoint);
			}
		}

		public event Action<Service> OnResponseReceived;

		public void Dispose()
		{
			lock (_socket)
			{
				_socket.Dispose();
				_isDisposed = true;
			}
		}
	}
}