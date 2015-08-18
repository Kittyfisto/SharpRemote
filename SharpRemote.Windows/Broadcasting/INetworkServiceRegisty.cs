using System.Collections.Generic;

namespace SharpRemote.Broadcasting
{
	internal interface INetworkServiceRegisty
	{
		IEnumerable<RegisteredService> GetServicesByName(string name);
	}
}