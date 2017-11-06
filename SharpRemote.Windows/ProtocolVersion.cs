using System;
using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     Defines the version of the sharpremote protocol.
	/// </summary>
	[Flags]
	[DataContract]
	public enum ProtocolVersion : byte
	{
		/// <summary>
		///     No version.
		/// </summary>
		[EnumMember] None = 0,

		/// <summary>
		///     1, the first version of the protocol which has been identified this way.
		/// </summary>
		/// <remarks>
		///     (There were many versions before this, but they weren't particularly user friendly
		///     and thus haven't been added to this enumeration).
		/// </remarks>
		[EnumMember] Version1 = 0x0001
	}
}