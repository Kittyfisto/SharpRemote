using System;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	/// Represents a socket that is capable of querying the network for services
	/// as well as to answer to queries as to which services are available.
	/// </summary>
	internal interface IServiceDiscoverySocket
	{
		void Query(string serviceName);
		event Action<Service> OnResponseReceived;
	}
}