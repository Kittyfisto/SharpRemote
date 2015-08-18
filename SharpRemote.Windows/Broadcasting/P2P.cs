using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;

namespace SharpRemote.Broadcasting
{
	/// <summary>
	/// </summary>
	internal static class P2P
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const int MaxQueries = 10;
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

		private static readonly ServiceRegistry Services;
		private static readonly AnyServiceDiscoverySocket Socket;

		static P2P()
		{
			Services = new ServiceRegistry();
			Socket = new AnyServiceDiscoverySocket(Services);
		}

		public static RegisteredService RegisterService(string name, IPEndPoint ep)
		{
			return Services.RegisterService(name, ep);
		}

		public static List<Service> FindAllServices(TimeSpan timeout)
		{
			return FindServices(null, timeout);
		}

		public static List<Service> FindAllServices()
		{
			return FindServices(null);
		}

		public static List<Service> FindServices(string name)
		{
			return FindServices(name, DefaultTimeout);
		}

		public static List<Service> FindServices(string name, TimeSpan timeout)
		{
			var ret = new HashSet<Service>();
			bool acceptingResponses = false;

			Action<Service> onResponse = service =>
			{
				lock (ret)
				{
					if (acceptingResponses)
						ret.Add(service);
				}
			};

			try
			{
				acceptingResponses = true;
				Socket.OnResponseReceived += onResponse;

				TimeSpan sleepTime = TimeSpan.FromSeconds(timeout.TotalSeconds / MaxQueries);
				for (int i = 0; i < MaxQueries; ++i)
				{
					lock (Socket)
					{
						Socket.Query(name);
					}

					Thread.Sleep(sleepTime);
				}
			}
			finally
			{
				lock (ret)
				{
					acceptingResponses = false;
					Socket.OnResponseReceived -= onResponse;
				}
			}

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Received '{0}' response(s): {1}",
					ret.Count,
					string.Join(", ", ret)
					);
			}

			return ret.ToList();
		}

		/*
		private const ushort Port = 65335;
		private const int MaxQueries = 10;
		private const int MaxLength = 508;
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
		private static readonly IPAddress MulticastAddress = IPAddress.Parse("239.255.255.255");
		private static readonly IPEndPoint MulticastEndPoint = new IPEndPoint(MulticastAddress, Port);

		private static readonly List<Socket> Sockets;
		private static readonly List<RegisteredService> Services;

		static P2P()
		{
			Sockets = new List<Socket>();
			var ifaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (var iface in ifaces)
			{
				var props = iface.GetIPProperties();
				foreach (var addr in props.UnicastAddresses)
				{
					var address = addr.Address;
					if (address.AddressFamily == AddressFamily.InterNetwork)
					{
						var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
						{
							MulticastLoopback = true,
						};
						socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
						socket.SetSocketOption(SocketOptionLevel.Socket,
													  SocketOptionName.ReuseAddress, true);
						socket.Bind(new IPEndPoint(address, Port));
						JoinMulticastGroup(socket, iface, address);
						Sockets.Add(socket);
					}
				}
			}

			BeginReceive();

			Services = new List<RegisteredService>();
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

		private static event Action<Service> OnResponse;

		private static void BeginReceive()
		{
			lock (Socket)
			{
				var buffer = new byte[MaxLength];
				EndPoint endPoint = new IPEndPoint(IPAddress.Any, Port);
				Socket.BeginReceiveFrom(buffer, 0, buffer.Length,
				                               SocketFlags.None,
				                               ref endPoint,
				                               OnReceived, buffer);
			}
		}

		private static void OnReceived(IAsyncResult ar)
		{
			try
			{
				EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, Port);
				int length;
				var buffer = (byte[]) ar.AsyncState;
				lock (Socket)
				{
					length = Socket.EndReceiveFrom(ar, ref remoteEndPoint);
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
							Action<Service> fn = OnResponse;
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

		private static void SendResponse(string name, EndPoint remoteEndPoint)
		{
			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Received query (name: {0}) from '{1}', answering...",
				                name,
				                remoteEndPoint);
			}

			var services = new List<RegisteredService>();
			lock (Services)
			{
				services.AddRange(Services.Where(service => name == NoName || service.Name == name));
			}

			lock (Socket)
			{
				foreach (var service in services)
				{
					byte[] buffer = Message.CreateResponse(service.Name, service.EndPoint);
					Socket.SendTo(buffer, MulticastEndPoint);
				}
			}
		}

		public static RegisteredService RegisterService(string name, IPEndPoint ep)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A name must be non-null and contain at least one character", "name");
			if (ep == null)
				throw new ArgumentNullException("ep");

			var service = new RegisteredService(name, ep);
			lock (Services)
			{
				Services.Add(service);
				service.OnDisposed += ServiceOnOnDisposed;
			}
			return service;
		}

		private static void ServiceOnOnDisposed(RegisteredService registeredService)
		{
			lock (Services)
			{
				Services.Remove(registeredService);
			}
		}

		public static List<Service> FindAllServices(TimeSpan timeout)
		{
			return FindServices(null, timeout);
		}

		public static List<Service> FindAllServices()
		{
			return FindServices(null);
		}

		public static List<Service> FindServices(string name)
		{
			return FindServices(name, DefaultTimeout);
		}

		public static List<Service> FindServices(string name, TimeSpan timeout)
		{
			var ret = new HashSet<Service>();
			byte[] query = Message.CreateQuery(name ?? NoName);
			bool acceptingResponses = false;

			Action<Service> onResponse = service =>
				{
					lock (ret)
					{
						if (acceptingResponses)
							ret.Add(service);
					}
				};

			try
			{
				acceptingResponses = true;
				OnResponse += onResponse;

				TimeSpan sleepTime = TimeSpan.FromSeconds(timeout.TotalSeconds/MaxQueries);
				for (int i = 0; i < MaxQueries; ++i)
				{
					lock (Socket)
					{
						Socket.SendTo(query, MulticastEndPoint);
					}

					Thread.Sleep(sleepTime);
				}
			}
			finally
			{
				lock (ret)
				{
					acceptingResponses = false;
					OnResponse -= onResponse;
				}
			}

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Received '{0}' response(s): {1}",
					ret.Count,
					string.Join(", ", ret)
					);
			}

			return ret.ToList();
		}
		 */
	}
}