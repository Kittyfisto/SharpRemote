﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace SharpRemote.ServiceDiscovery
{
	internal sealed class ServiceRegistry
		: INetworkServiceRegisty
		, IEnumerable<RegisteredService>
	{
		private readonly List<RegisteredService> _services;
		private readonly object _syncRoot;

		public ServiceRegistry()
		{
			_syncRoot = new object();
			_services = new List<RegisteredService>();
		}

		public RegisteredService RegisterService(string name, IPEndPoint endPoint, byte[] payload = null)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			if (name == "")
				throw new ArgumentException("A name must consist of at least one character", nameof(name));
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));

			var service = new RegisteredService(name, endPoint, payload);
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

		public IEnumerator<RegisteredService> GetEnumerator()
		{
			lock (_syncRoot)
			{
				return _services.ToList().GetEnumerator();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}