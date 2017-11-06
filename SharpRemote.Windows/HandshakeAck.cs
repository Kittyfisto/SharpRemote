using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     This structure forms the second part of the client/server handshake:
	///     It is sent from the server to the client (in response to <see cref="HandshakeSyn" />)
	///     and informs the client about the server's choice of protocol version and serializer.
	/// </summary>
	[DataContract]
	public struct HandshakeAck
	{
		/// <summary>
		///     The version the sharpremote protocol the server wants to use for this connection.
		///     If the server doesn't support any of the client's versions, then <see cref="ProtocolVersion.None" />
		///     is returned here.
		/// </summary>
		[DataMember] public ProtocolVersion Version;

		/// <summary>
		///     The type of serializer the server wants to use for this connection.
		///     If the server doesn't support any of the client's serializers, then <see cref="SharpRemote.Serializer.None" />
		///     is returned here.
		/// </summary>
		[DataMember] public Serializer Serializer;

		/// <summary>
		///     The response to the client's challenge or null if the client didn't pose a challenge
		///     *or* the response couldn't be created.
		/// </summary>
		[DataMember] public object Response;

		/// <summary>
		///     The challenge that is posed by the server (and must be answered by the client).
		/// </summary>
		[DataMember] public object Challenge;
	}
}