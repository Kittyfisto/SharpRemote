using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using log4net;

namespace SharpRemote.Broadcasting
{
	/// <summary>
	/// Service discovery socket that is bound to any address (and thus any network interface).
	/// </summary>
	internal sealed class AnyServiceDiscoverySocket
		: IServiceDiscoverySocket
		  , IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly INetworkServiceRegisty _services;
		private readonly Dictionary<IPAddress, ServiceDiscoverySocket> _sockets;
		private readonly object _syncRoot;

		public AnyServiceDiscoverySocket(INetworkServiceRegisty services)
		{
			if (services == null)
				throw new ArgumentNullException("services");

			_services = services;
			_sockets = new Dictionary<IPAddress, ServiceDiscoverySocket>();
			_syncRoot = new object();

			NetworkChange.NetworkAddressChanged += NetworkChangeOnNetworkAddressChanged;
			BindToAllAddresses();
		}

		private void NetworkChangeOnNetworkAddressChanged(object sender, EventArgs eventArgs)
		{
			BindToAllAddresses();
		}

		private void SocketOnResponseReceived(Service service)
		{
			var fn = OnResponseReceived;
			if (fn != null)
				fn(service);
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				foreach (ServiceDiscoverySocket socket in _sockets.Values)
				{
					socket.Dispose();
				}
				_sockets.Clear();

				NetworkChange.NetworkAddressChanged -= NetworkChangeOnNetworkAddressChanged;
			}
		}

		public void Query(string serviceName)
		{
			lock (_syncRoot)
			{
				foreach (ServiceDiscoverySocket socket in _sockets.Values)
				{
					socket.Query(serviceName);
				}
			}
		}

		public event Action<Service> OnResponseReceived;

		private void BindToAllAddresses()
		{
			lock (_syncRoot)
			{
				NetworkInterface[] ifaces = NetworkInterface.GetAllNetworkInterfaces();
				var currentAddresses = new HashSet<IPAddress>();

				//
				// #1: Add sockets for addresses that we haven't bound to (yet)...
				//
				foreach (NetworkInterface iface in ifaces)
				{
					var status = iface.OperationalStatus;
					if (status != OperationalStatus.Up)
					{
						if (Log.IsDebugEnabled)
							Log.DebugFormat("Ignoring network interface {0} because it's status is {1}",
							                iface.Name,
							                status);
					}
					else
					{
					IPInterfaceProperties props = iface.GetIPProperties();
					foreach (UnicastIPAddressInformation addr in props.UnicastAddresses)
					{
						IPAddress address = addr.Address;
						if (address.AddressFamily == AddressFamily.InterNetwork)
						{
							ServiceDiscoverySocket socket;
							if (!_sockets.TryGetValue(address, out socket))
							{
								Log.InfoFormat("Creating service discovery socket for {0}@{1}",
								               address,
								               iface.Name);

								socket = new ServiceDiscoverySocket(iface, address, _services);
								_sockets.Add(address, socket);
								socket.OnResponseReceived += SocketOnResponseReceived;
							}

							currentAddresses.Add(address);
						}
					}
					}
				}

				//
				// #2: Remove sockets for addresses that are no longer in use...
				//
				foreach (var pair in _sockets.ToList())
				{
					if (!currentAddresses.Contains(pair.Key))
					{
						pair.Value.Dispose();
						pair.Value.OnResponseReceived -= SocketOnResponseReceived;
						_sockets.Remove(pair.Key);
					}
				}
			}
		}
	}
}