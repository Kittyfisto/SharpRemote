using System;
using System.Collections.Generic;
using System.Net;

namespace SharpRemote
{
	/// <summary>
	///     Represents an endpoint in that can establish a connection to exactly one other
	///     endpoint in the same / different AppDomain, Process or Machine.
	/// </summary>
	/// <remarks>
	///     There can only be one endpoint *per* <see cref="IPEndPoint" /> *per* machine.
	/// </remarks>
	public interface IRemotingEndPoint
		: IRemotingBase
	{
		/// <summary>
		///     Whether or not this endpoint is connected to another one.
		/// </summary>
		bool IsConnected { get; }
		
		/// <summary>
		///     The total number of <see cref="IServant" />s that have been removed from this endpoint because
		///     their subjects have been collected by the GC.
		/// </summary>
		long NumServantsCollected { get; }
		
		/// <summary>
		///     The total number of <see cref="IProxy" />s that have been removed from this endpoint because
		///     they're no longer used.
		/// </summary>
		long NumProxiesCollected { get; }

		/// <summary>
		/// The id of the current connection or <see cref="ConnectionId.None"/> if no connection
		/// is currently established.
		/// </summary>
		ConnectionId CurrentConnectionId { get; }

		/// <summary>
		/// 
		/// </summary>
		EndPoint RemoteEndPoint { get; }

		/// <summary>
		///     Returns all the proxies of this endpoint.
		/// </summary>
		IEnumerable<IProxy> Proxies { get; }

		/// <summary>
		/// Is called when a connection with another <see cref="IRemotingEndPoint"/>
		/// is created.
		/// </summary>
		/// <remarks>
		/// The event is fired with the endpoint of the *other* <see cref="IRemotingEndPoint"/>.
		/// </remarks>
		event Action<EndPoint, ConnectionId> OnConnected;

		/// <summary>
		/// Is called when a connection with another <see cref="IRemotingEndPoint"/> is disconnected.
		/// </summary>
		event Action<EndPoint, ConnectionId> OnDisconnected;

		/// <summary>
		///     This event is invoked right before a socket is to be closed due to failure of:
		///     - the connection between endpoints
		///     - a failure of the remote process
		///     - a failure of SharpRemote
		///     - something else ;)
		/// </summary>
		event Action<EndPointDisconnectReason, ConnectionId> OnFailure;

		/// <summary>
		///     Disconnects this endpoint from its remote endpoint.
		/// </summary>
		/// <remarks>
		///     When this endpoint is not connected to a remot endpoint in the first place, then this method does nothing.
		/// </remarks>
		void Disconnect();

		
		#region Servant registration

		/// <summary>
		///     Creates and registers a servant for the given subject <paramref name="subject" /> under the given
		///     <paramref name="objectId" />,
		///     giving another connected <see cref="IRemotingEndPoint" /> the ability to call the subject's methods via a proxy
		///     (<see cref="IRemotingEndPoint.CreateProxy{T}" />).
		/// </summary>
		/// <remarks>
		///     A servant is responsible for invoking RPCs on the original subject whenever they are called through a proxy.
		///     If the interfaces <typeparamref name="T"/> methods are attributed with the <see cref="InvokeAttribute"/>,
		///     then the servant will ensure that methods are invoked with the specified synchronization.
		/// </remarks>
		/// <remarks>
		///     A servant can be created independent from any proxy and the order in which both are created is unimportant, for as
		///     long as no interface methods / properties are invoked.
		/// </remarks>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <param name="subject"></param>
		/// <returns></returns>
		IServant CreateServant<T>(ulong objectId, T subject) where T : class;

		/// <summary>
		///     Retrieves the subject that was previously registered at this end-point via <see cref="CreateServant{T}" />
		///     (and has not yet been garbage collected).
		/// </summary>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId">The objectId that has been given to this endpoint when registering a servant for the subject</param>
		/// <returns>The subject that was registered or null if the subject has been garbage collected already</returns>
		T RetrieveSubject<T>(ulong objectId) where T : class;

		/// <summary>
		///     If a servant has already been registered (via <see cref="CreateServant{T}" /> or this method),
		///     then it is returned. Otherwise a new servant will be registered.
		/// </summary>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="subject"></param>
		/// <returns></returns>
		IServant GetExistingOrCreateNewServant<T>(T subject) where T : class;

		#endregion

		#region Proxy Creation

		/// <summary>
		///     Creates and registers an object that implements the given interface <typeparamref name="T" />.
		///     Calls to properties / methods of the given interface are marshalled to connected endpoint, if an appropriate
		///     servant of the same interface an <paramref name="objectId" /> has been created using <see cref="CreateServant{T}" />.
		/// </summary>
		/// <remarks>
		///     A proxy can be created independent from its servant and the order in which both are created is unimportant, for as long
		///     as no interface methods / properties are invoked.
		/// </remarks>
		/// <remarks>
		///     Every method / property on the given object is now capable of throwing an additional set of exceptions, in addition
		///     to whatever exceptions any implementation already throws:
		///     - <see cref="NoSuchServantException" />: There's no servant with the id of the proxy and therefore no subject on which the method could possibly be executed
		///     - <see cref="NotConnectedException" />: At the time of calling the proxy's method, no connection to a remote end point was available
		///     - <see cref="ConnectionLostException" />: The method call was cancelled because the connection between proxy and servant was interrupted / lost / disconnected
		///     - <see cref="UnserializableException" />: The remote method was executed, threw an exception, but the exception could not be serialized
		/// </remarks>
		/// <remarks>
		///     This method is thread-safe.
		/// </remarks>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">
		///     When there already exists a proxy of id <paramref name="objectId" />.
		/// </exception>
		/// <exception cref="ArgumentException">
		///     When <typeparamref name="T" /> does not refer to an interface.
		/// </exception>
		T CreateProxy<T>(ulong objectId) where T : class;

		/// <summary>
		/// Returns the proxy that belongs to the given object-id.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">When there's no proxy with that id</exception>
		/// <exception cref="ArgumentException">When there proxy's type does not match the given <typeparamref name="T"/> type parameter</exception>
		T GetProxy<T>(ulong objectId) where T : class;

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class;

		#endregion
	};
}