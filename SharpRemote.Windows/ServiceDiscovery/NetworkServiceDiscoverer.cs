using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using log4net;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	/// Responsible for performing queries of services in the local network as well as to answer
	/// those queries.
	/// </summary>
	public sealed class NetworkServiceDiscoverer
		: INetworkServiceDiscoverer
		, IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const int MaxQueries = 10;
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

		private readonly ServiceRegistry _services;
		private readonly ServiceDiscoveryAnySocket _socket;
		private readonly NetworkServiceDiscoverySettings _settings;
		private readonly object _syncRoot;
		private bool _isDisposed;

		/// <summary>
		/// Whether or not <see cref="Dispose"/> has been called.
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		/// Initializes a new instance of this class with the specified settings, or
		/// default values when none are given.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="sendLegacyResponse">Whether or not legacy responses should be sent. When this is set to true, then SharpRemote V0.3 (and earlier) is able to understand a response sent from this discoverer.</param>
		public NetworkServiceDiscoverer(NetworkServiceDiscoverySettings settings = null,
										bool sendLegacyResponse = false)
		{
			_settings = settings ?? new NetworkServiceDiscoverySettings();
			_services = new ServiceRegistry();
			_socket = new ServiceDiscoveryAnySocket(_services,
			                                        _settings.MulticastAddress,
			                                        _settings.Port,
			                                        _settings.TTL,
			                                        sendLegacyResponse);
			_syncRoot = new object();
		}

		/// <summary>
		/// The services registered at this instance.
		/// </summary>
		public IEnumerable<RegisteredService> LocalServices => _services.ToList();

		/// <summary>
		/// The settings this service operates under.
		/// Cannot be changed after the fact.
		/// </summary>
		public NetworkServiceDiscoverySettings Settings => _settings;

		/// <summary>
		/// Registers a new service with the given name and endPoint.
		/// The given service remains registered and therefore is discoverable
		/// until either:
		/// - the service is disposed of
		/// - the AppDomain is shut down
		/// </summary>
		/// <remarks>
		/// There can only be one one service per (name, endPoint) tuple.
		/// Registering the same tuple again throws.
		/// </remarks>
		/// <param name="name">The name of the service</param>
		/// <param name="endPoint">The endpoint the service can be contacted at</param>
		/// <param name="payload">An optional payload</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">When <paramref name="name"/> or <paramref name="endPoint"/> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="name"/> is empty</exception>
		public RegisteredService RegisterService(string name, IPEndPoint endPoint, string payload = null)
		{
			lock (_syncRoot)
			{
				if (_isDisposed)
					throw new ObjectDisposedException("");

				return _services.RegisterService(name, endPoint, payload);
			}
		}

		/// <summary>
		/// Finds all services with the given name in the local network that respond within the one second.
		/// </summary>
		/// <remarks>
		/// Blocks for at least one second, but not much longer.
		/// </remarks>
		/// <returns></returns>
		public List<Service> FindAllServices()
		{
			return FindServices(null);
		}

		/// <summary>
		/// Finds all services in the local network that respond within the given time span.
		/// </summary>
		/// <remarks>
		/// Blocks for at least the given timeout, but not much longer.
		/// </remarks>
		/// <param name="timeout">The amount of time this method should wait for a response</param>
		/// <returns></returns>
		public List<Service> FindAllServices(TimeSpan timeout)
		{
			return FindServices(null, timeout);
		}

		/// <summary>
		/// Finds all services with the given name in the local network that respond within one second.
		/// </summary>
		/// <remarks>
		/// Blocks for at least one second, but not much longer.
		/// </remarks>
		/// <param name="name">The name of the service to look for - case sensitive</param>
		/// <returns></returns>
		public List<Service> FindServices(string name)
		{
			return FindServices(name, DefaultTimeout);
		}

		/// <summary>
		/// Finds all services with the given name in the local network that respond within the given time span.
		/// </summary>
		/// <remarks>
		/// Blocks for at least the given timeout, but not much longer.
		/// </remarks>
		/// <param name="name">The name of the service to look for - case sensitive</param>
		/// <param name="timeout">The amount of time this method should wait for a response</param>
		/// <returns></returns>
		public List<Service> FindServices(string name, TimeSpan timeout)
		{
			if (timeout <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(timeout));

			lock (_syncRoot)
			{
				if (_isDisposed)
					throw new ObjectDisposedException("");

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
					_socket.OnResponseReceived += onResponse;

					TimeSpan sleepTime = TimeSpan.FromSeconds(timeout.TotalSeconds / MaxQueries);
					for (int i = 0; i < MaxQueries; ++i)
					{
						lock (_socket)
						{
							_socket.Query(name);
						}

						Thread.Sleep(sleepTime);
					}
				}
				finally
				{
					lock (ret)
					{
						acceptingResponses = false;
						_socket.OnResponseReceived -= onResponse;
					}
				}

				if (Log.IsDebugEnabled)
				{
					Log.DebugFormat("Received '{0}' response(s): {1}",
						ret.Count,
						string.Join(", ", ret)
						);
				}

				RemoveDuplicateLegacyResponses(ret);
				return ret.ToList();
			}
		}

		private void RemoveDuplicateLegacyResponses(HashSet<Service> responses)
		{
			foreach (var service in responses.ToList())
			{
				if (service.Payload != null)
				{
					var serviceWithoutPayload = new Service(service.Name, service.EndPoint, service.LocalAddress, service.NetworkInterfaceId);
					if (responses.Contains(serviceWithoutPayload))
						responses.Remove(serviceWithoutPayload);
				}
			}
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (_syncRoot)
			{
				_socket.Dispose();
				_isDisposed = true;
			}
		}
	}
}