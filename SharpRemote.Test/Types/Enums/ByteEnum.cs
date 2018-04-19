using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum ByteEnum : byte
	{
		[EnumMember] A = 0,
		[EnumMember] B = byte.MaxValue / 2,
		[EnumMember] C = byte.MaxValue
	}
}