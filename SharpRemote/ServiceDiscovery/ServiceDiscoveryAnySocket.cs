using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using log4net;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	/// Service discovery socket that is bound to any address (and thus any network interface).
	/// </summary>
	internal sealed class ServiceDiscoveryAnySocket
		: IServiceDiscoverySocket
		  , IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly INetworkServiceRegisty _services;
		private readonly IPAddress _multicastAddress;
		private readonly int _port;
		private readonly bool _sendLegacyResponse;
		private readonly int _ttl;
		private readonly Dictionary<IPAddress, ServiceDiscoverySocket> _sockets;
		private readonly object _syncRoot;

		public ServiceDiscoveryAnySocket(INetworkServiceRegisty services,
		                                 IPAddress multicastAddress,
		                                 int port,
		                                 int ttl,
		                                 bool sendLegacyResponse)
		{
			if (services == null)
				throw new ArgumentNullException(nameof(services));
			if (multicastAddress == null)
				throw new ArgumentNullException(nameof(multicastAddress));
			if (port <= 0 || port >= ushort.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(port));
			if (ttl <= 0 || ttl >= byte.MaxValue)
				throw new ArgumentOutOfRangeException(nameof(ttl));

			_services = services;
			_multicastAddress = multicastAddress;
			_port = port;
			_ttl = ttl;
			_sendLegacyResponse = sendLegacyResponse;
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
			OnResponseReceived?.Invoke(service);
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
				try
				{
					IEnumerable<NetworkInterface> ifaces = GetAllNetworkInterfaces();
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
										if (Log.IsDebugEnabled)
											Log.DebugFormat("Creating service discovery socket for {0}@{1}",
											                address,
											                iface.Name);

										try
										{
											socket = new ServiceDiscoverySocket(iface,
																				address,
																				_multicastAddress,
																				_port,
																				_ttl,
																				_services,
																				_sendLegacyResponse);
										}
										catch (SocketException e)
										{
											Log.WarnFormat("Caught unexpected exception while creating service discovery socket for {0}@{1}: {2}",
															addr,
															iface.Name,
															e);
										}

										if (socket != null)
										{
											_sockets.Add(address, socket);
											socket.OnResponseReceived += SocketOnResponseReceived;

											Log.InfoFormat("Created service discovery socket for {0}@{1}",
											               address,
											               iface.Name);
										}
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
						var socket = pair.Value;
						var address = pair.Key;
						if (!currentAddresses.Contains(address))
						{
							if (Log.IsDebugEnabled)
								Log.DebugFormat("Removing service discovery socket for {0}@{1}",
								                address,
								                socket.InterfaceName);

							_sockets.Remove(address);

							Log.InfoFormat("Removed service discovery socket for {0}@{1}, current status: {2}",
							               address,
							               socket.InterfaceName,
							               socket.InterfaceStatus);

							socket.Dispose();
							socket.OnResponseReceived -= SocketOnResponseReceived;
						}
					}
				}
				catch (Exception e)
				{
					// This method is invoked from callbacks which is why we're not allowed to throw any exception
					Log.ErrorFormat("Caught unexpected exception while binding socket to all adapters: {0}", e);
				}
			}
		}

		/// <summary>
		/// Retrieves the list of all current network interfaces on this computer.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<NetworkInterface> GetAllNetworkInterfaces()
		{
			// For some reason I do not know, the operation may fail, therefore
			// we're preparing for this scenario!
			var backoffTimes = new[]
				{
					TimeSpan.FromMilliseconds(10),
					TimeSpan.FromMilliseconds(100),
					TimeSpan.FromMilliseconds(1000)
				};

			NetworkInterface[] ifaces = null;
			foreach (var backoffTime in backoffTimes)
			{
				try
				{
					ifaces = NetworkInterface.GetAllNetworkInterfaces();
					break;
				}
				catch (NetworkInformationException e)
				{
					Log.WarnFormat("Unable to retrieve all network interfaces, trying again in {0}: {1}",
					               backoffTime,
					               e);
					Thread.Sleep(backoffTime);
				}
				catch (Exception e)
				{
					Log.ErrorFormat("Unable to retrieve all network interfaces: {0}",
								   e);
					break;
				}
			}

			return ifaces ?? new NetworkInterface[0];
		}
	}
}