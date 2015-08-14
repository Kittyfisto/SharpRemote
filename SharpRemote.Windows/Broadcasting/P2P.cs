using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using log4net;

namespace SharpRemote.Broadcasting
{
	/// <summary>
	/// 
	/// </summary>
	internal static class P2P
	{
		private const ushort Port = 4567;
		private const int MaxQueries = 10;
		private const int MaxLength = 508;
		private const string NoName = "";
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
		private static readonly IPAddress MulticastAddress = IPAddress.Parse("239.255.255.255");

		private static readonly Socket SendSocket;
		private static readonly Socket ReceiveSocket;
		private static readonly List<RegisteredService> Services;

		static P2P()
		{
			SendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
				{
					DontFragment = true,
					MulticastLoopback = true,
				};
			SendSocket.SetSocketOption(SocketOptionLevel.IP,
			                           SocketOptionName.AddMembership,
			                           new MulticastOption(MulticastAddress));
			SendSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 2);
			SendSocket.Connect(new IPEndPoint(MulticastAddress, Port));

			ReceiveSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
				{
					MulticastLoopback = true,
				};
			ReceiveSocket.SetSocketOption(SocketOptionLevel.Socket,
			                              SocketOptionName.ReuseAddress, true);
			ReceiveSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
			ReceiveSocket.SetSocketOption(SocketOptionLevel.IP,
			                              SocketOptionName.AddMembership,
			                              new MulticastOption(MulticastAddress));


			BeginReceive();

			Services = new List<RegisteredService>();
		}

		private static event Action<Service> OnResponse;

		private static void BeginReceive()
		{
			lock (ReceiveSocket)
			{
				var buffer = new byte[MaxLength];
				ReceiveSocket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, OnReceived, buffer);
			}
		}

		private static void OnReceived(IAsyncResult ar)
		{
			try
			{
				int length;
				var buffer = (byte[]) ar.AsyncState;
				lock (ReceiveSocket)
				{
					SocketError error;
					length = ReceiveSocket.EndReceive(ar, out error);
				}

				string token;
				string name;
				IPEndPoint endPoint;
				if (Message.TryRead(buffer, out token, out name, out endPoint))
				{
					switch (token)
					{
						case Message.P2PQueryToken:
							SendResponse(name);
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
						Log.DebugFormat("Received invalid response ({0} bytes), ignoring it", length);
					}
				}
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

		private static void SendResponse(string name)
		{
			var services = new List<RegisteredService>();
			lock (Services)
			{
				services.AddRange(Services.Where(service => name == NoName || service.Name == name));
			}

			lock (SendSocket)
			{
				foreach (var service in services)
				{
					byte[] buffer = Message.CreateResponse(service.Name, service.EndPoint);
					SendSocket.Send(buffer);
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
					lock (SendSocket)
					{
						SendSocket.Send(query);
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

			return ret.ToList();
		}
	}
}