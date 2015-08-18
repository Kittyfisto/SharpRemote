using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using log4net;

namespace SharpRemote.Broadcasting
{
	/// <summary>
	/// Service discovery socket implementation that is bound to a single address (and thus to a single network interface).
	/// </summary>
	internal sealed class ServiceDiscoverySocket
		: IServiceDiscoverySocket
		, IDisposable
	{
		private readonly INetworkServiceRegisty _services;
		private const ushort Port = 65335;
		private const int MaxLength = 508;
		private const string NoName = "";

		private static readonly IPAddress MulticastAddress = IPAddress.Parse("239.255.255.255");
		private static readonly IPEndPoint MulticastEndPoint = new IPEndPoint(MulticastAddress, Port);

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Socket _socket;
		private bool _isDisposed;

		public ServiceDiscoverySocket(NetworkInterface iface, IPAddress address, INetworkServiceRegisty services)
		{
			_services = services;
			_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
				{
					MulticastLoopback = true,
				};
			_socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
			_socket.SetSocketOption(SocketOptionLevel.Socket,
			                        SocketOptionName.ReuseAddress, true);
			_socket.Bind(new IPEndPoint(address, Port));
			JoinMulticastGroup(_socket, iface, address);

			BeginReceive();
		}

		private static void JoinMulticastGroup(Socket socket, NetworkInterface iface, IPAddress address)
		{
			if (address.AddressFamily == AddressFamily.InterNetwork)
			{
				try
				{
					socket.SetSocketOption(SocketOptionLevel.IP,
					                       SocketOptionName.AddMembership,
					                       new MulticastOption(MulticastAddress,
					                                           address));

					Log.DebugFormat("Joined multicast group {0} for {1}@{2}",
					                MulticastAddress,
					                address,
					                iface.Name);
				}
				catch (SocketException e)
				{
					Log.DebugFormat("Unable to join multicast group {0} for {1}@{2}: {3}",
					                MulticastAddress,
					                address,
					                iface.Name,
					                e);
				}
			}
		}

		private void BeginReceive()
		{
			lock (_socket)
			{
				if (_isDisposed)
					return;

				var buffer = new byte[MaxLength];
				EndPoint endPoint = new IPEndPoint(IPAddress.Any, Port);
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
				EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, Port);
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
								fn(new Service(name, endPoint));
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
					byte[] buffer = Message.CreateResponse(service.Name, service.EndPoint);
					_socket.SendTo(buffer, MulticastEndPoint);
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

				_socket.SendTo(query, MulticastEndPoint);
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