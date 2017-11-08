using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     The reason why the server dropped the connection to the client:
	///     Is only used as part of the connection handshake between client and server.
	/// </summary>
	[DataContract]
	public enum ConnectionDropReason
	{
		/// <summary>
		///     The connection isn't being dropped.
		/// </summary>
		[EnumMember] None = 0,

		/// <summary>
		///     The server doesn't accept the protocol version choices offered by the client.
		/// </summary>
		[EnumMember] UnacceptableVersion = 1,

		/// <summary>
		///     The server doesn't accept the serializer choices offered by the client.
		/// </summary>
		[EnumMember] UnacceptableSerializer = 2,

		/// <summary>
		///     The server doesn't accept the type model sent by the client.
		///     This may be because the type model uses types the server doesn't want to use,
		///     or cannot resolve, etc...
		/// </summary>
		[EnumMember] UnacceptableTypeModel = 3
	}
}