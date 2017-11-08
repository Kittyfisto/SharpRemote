using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     This structure forms the first part of the client/server handshake:
	///     It is sent from the client to the server upon establishing a connection and
	///     informs the server about the client's capabilities.
	/// </summary>
	/// <remarks>
	///     TODO: Is there a better name for these types? Syn, Ack, Synack is known, but it also part of TCP so these names
	///     might be misleading.
	/// </remarks>
	[DataContract]
	public struct HandshakeSyn
	{
		/// <summary>
		///     The versions of the sharpremote protocol supported by the client.
		/// </summary>
		[DataMember] public ProtocolVersion SupportedVersions;

		/// <summary>
		///     The serializers supported by the client.
		/// </summary>
		[DataMember] public Serializer SupportedSerializers;

		/// <summary>
		///     A model of all types the client expects the server to know.
		/// </summary>
		[DataMember] public TypeModel TypeModel;

		/// <summary>
		///     The challenge that is posed by the client (and must be answered by the server).
		/// </summary>
		[DataMember] public object Challenge;
	}
}