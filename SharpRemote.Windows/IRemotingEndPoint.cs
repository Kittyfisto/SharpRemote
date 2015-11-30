using System;
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
		: IDisposable
	{
		/// <summary>
		///     The name of this endpoint, only used for debugging.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     Whether or not this endpoint is connected to another one.
		/// </summary>
		bool IsConnected { get; }

		/// <summary>
		/// The current average round trip time or <see cref="TimeSpan.Zero"/> in
		/// case nothing was measured.
		/// </summary>
		TimeSpan RoundtripTime { get; }

		/// <summary>
		/// 
		/// </summary>
		EndPoint LocalEndPoint { get; }

		/// <summary>
		/// 
		/// </summary>
		EndPoint RemoteEndPoint { get; }

		/// <summary>
		/// Is called when a connection with another <see cref="AbstractSocketRemotingEndPoint"/>
		/// is created.
		/// </summary>
		/// <remarks>
		/// The event is fired with the endpoint of the *other* <see cref="AbstractSocketRemotingEndPoint"/>.
		/// </remarks>
		event Action<EndPoint> OnConnected;

		/// <summary>
		/// Is called when a connection with another <see cref="AbstractSocketRemotingEndPoint"/> is disconnected.
		/// </summary>
		event Action<EndPoint> OnDisconnected;

		/// <summary>
		///     This event is invoked right before a socket is to be closed due to failure of:
		///     - the connection between endpoints
		///     - a failure of the remote process
		///     - a failure of SharpRemote
		///     - something else ;)
		/// </summary>
		event Action<EndPointDisconnectReason> OnFailure;

		/// <summary>
		///     Disconnects this endpoint from its remote endpoint.
		/// </summary>
		/// <remarks>
		///     When this endpoint is not connected to a remot endpoint in the first place, then this method does nothing.
		/// </remarks>
		void Disconnect();

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
		///     Creates and registers an object for the given subject <paramref name="subject" /> and invokes its methods, when they
		///     have been called on the corresponding proxy.
		/// </summary>
		/// <remarks>
		///     A servant can be created independent from any proxy and the order in which both are created is unimportant, for as long
		///     as no interface methods / properties are invoked.
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
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <returns></returns>
		T GetExistingOrCreateNewProxy<T>(ulong objectId) where T : class;

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="subject"></param>
		/// <returns></returns>
		IServant GetExistingOrCreateNewServant<T>(T subject) where T : class;
	};
}