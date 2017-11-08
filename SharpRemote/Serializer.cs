using System;
using System.Runtime.Serialization;

namespace SharpRemote
{
	/// <summary>
	///     Defines the type of serializer used/supported.
	/// </summary>
	[Flags]
	[DataContract]
	public enum Serializer : byte
	{
		/// <summary>
		///     No serializer.
		/// </summary>
		[EnumMember] None = 0,

		/// <summary>
		///     A binary serializer which produces small, but not human-readable output.
		/// </summary>
		[EnumMember] BinarySerializer = 0x0001

		// <summary>
		// NOT YET SUPPORTED.
		// </summary>
		//[EnumMember]
		//XmlSerializer = 0x0002,

		// <summary>
		// NOT YET SUPPORTED.
		// </summary>
		//[EnumMember]
		//JsonSerializer = 0x0004
	}
}