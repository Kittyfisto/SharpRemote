using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     This structure forms the third and final part of the client/server handshake:
	///     It is send from the client as a response to <see cref="HandshakeAck" /> and confirms
	///     that the connection is now established.
	/// </summary>
	[DataContract]
	public struct HandshakeSynack
	{
		/// <summary>
		///     The response to the server's challenge or null if the server didn't pose a challenge
		///     *or* the response couldn't be created.
		/// </summary>
		[DataMember] public object Response;
	}
}