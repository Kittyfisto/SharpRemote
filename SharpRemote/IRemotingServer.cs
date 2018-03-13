using System;
using System.Collections.Generic;

namespace SharpRemote
{
	/// <summary>
	///     The interface for a server-side remoting interface:
	///     Central server which can accept many connections.
	/// </summary>
	public interface IRemotingServer
		: IRemotingBase
	{
		/// <summary>
		///     The current list of connections to this server.
		/// </summary>
		IEnumerable<IRemotingEndPoint> Connections { get; }

		/// <summary>
		///     This event is fired whenever a new connection has been (successfully) established.
		/// </summary>
		event Action<IRemotingEndPoint> OnClientConnected;

		/// <summary>
		///     This event is fired whenever a connection has been disconnected.
		/// </summary>
		/// <remarks>
		///     Is called when:
		///     - Client disconnected
		///     - Server disconnected
		///     - The underlying transport channel failed
		/// </remarks>
		event Action<IRemotingEndPoint> OnClientDisconnected;

		/// <summary>
		///     Registers a servant for the given subject.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectId"></param>
		/// <param name="subject"></param>
		void CreateServant<T>(ulong objectId, T subject) where T : class;
	}
}