using System.Collections.Generic;

namespace SharpRemote.ServiceDiscovery
{
	internal interface INetworkServiceRegisty
	{
		IEnumerable<RegisteredService> GetServicesByName(string name);
	}
}