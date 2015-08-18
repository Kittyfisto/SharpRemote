using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SharpRemote.Broadcasting
{
	internal sealed class ServiceRegistry
		: INetworkServiceRegisty
	{
		private readonly List<RegisteredService> _services;
		private readonly object _syncRoot;

		public ServiceRegistry()
		{
			_syncRoot = new object();
			_services = new List<RegisteredService>();
		}

		public RegisteredService RegisterService(string name, IPEndPoint ep)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("A name must be non-null and contain at least one character", "name");
			if (ep == null)
				throw new ArgumentNullException("ep");

			var service = new RegisteredService(name, ep);
			lock (_syncRoot)
			{
				_services.Add(service);
				service.OnDisposed += ServiceOnOnDisposed;
			}

			return service;
		}

		private void ServiceOnOnDisposed(RegisteredService registeredService)
		{
			lock (_syncRoot)
			{
				_services.Remove(registeredService);
			}
		}

		public IEnumerable<RegisteredService> GetServicesByName(string name)
		{
			lock (_syncRoot)
			{
				return _services.Where(x => x.Name == name || x.Name == "");
			}
		}
	}
}