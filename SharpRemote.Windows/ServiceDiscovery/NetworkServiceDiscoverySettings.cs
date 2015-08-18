using System.Net;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	/// Configuration of a <see cref="NetworkServiceDiscoverer"/>.
	/// </summary>
	public sealed class NetworkServiceDiscoverySettings
	{
		/// <summary>
		/// Creates a new instance of this class with all fields set to their default values.
		/// </summary>
		public NetworkServiceDiscoverySettings()
		{
			Port = 65335;
			MulticastAddress = IPAddress.Parse("239.255.255.255");
			TTL = 2;
		}

		/// <summary>
		/// The port used by the network service discoverer.
		/// </summary>
		/// <remarks>
		/// Defaults to 65335.
		/// </remarks>
		public int Port;

		/// <summary>
		/// The address of the multicast group used by all network service discoverers.
		/// </summary>
		/// <remarks>
		/// Defaults to 239.255.255.255.
		/// </remarks>
		public IPAddress MulticastAddress;

		/// <summary>
		/// The maximum number of hops the network discovery service takes.
		/// </summary>
		public int TTL;
	}
}