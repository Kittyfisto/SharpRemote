using System;
using System.Collections.Generic;
using System.Net;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	///     Responsible for performing queries of services in the local network as well as to answer
	///     those queries.
	/// </summary>
	public interface INetworkServiceDiscoverer
	{
		/// <summary>
		/// The services registered at this instance.
		/// </summary>
		IEnumerable<RegisteredService> LocalServices { get; }

		/// <summary>
		/// The settings this service operates under.
		/// Cannot be changed after the fact.
		/// </summary>
		NetworkServiceDiscoverySettings Settings { get; }

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
		/// <param name="name"></param>
		/// <param name="endPoint"></param>
		/// <param name="payload">An optional payload</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">When <paramref name="name"/> or <paramref name="endPoint"/> is null</exception>
		/// <exception cref="ArgumentException">When <paramref name="name"/> is empty</exception>
		RegisteredService RegisterService(string name, IPEndPoint endPoint, byte[] payload = null);

		/// <summary>
		/// Finds all services with the given name in the local network that respond within the one second.
		/// </summary>
		/// <remarks>
		/// Blocks for at least one second, but not much longer.
		/// </remarks>
		/// <returns></returns>
		List<Service> FindAllServices();

		/// <summary>
		/// Finds all services in the local network that respond within the given time span.
		/// </summary>
		/// <remarks>
		/// Blocks for at least the given timeout, but not much longer.
		/// </remarks>
		/// <param name="timeout">The amount of time this method should wait for a response</param>
		/// <returns></returns>
		List<Service> FindAllServices(TimeSpan timeout);

		/// <summary>
		/// Finds all services with the given name in the local network that respond within one second.
		/// </summary>
		/// <remarks>
		/// Blocks for at least one second, but not much longer.
		/// </remarks>
		/// <param name="name">The name of the service to look for - case sensitive</param>
		/// <returns></returns>
		List<Service> FindServices(string name);

		/// <summary>
		/// Finds all services with the given name in the local network that respond within the given time span.
		/// </summary>
		/// <remarks>
		/// Blocks for at least the given timeout, but not much longer.
		/// </remarks>
		/// <param name="name">The name of the service to look for - case sensitive</param>
		/// <param name="timeout">The amount of time this method should wait for a response</param>
		/// <returns></returns>
		List<Service> FindServices(string name, TimeSpan timeout);
	}
}