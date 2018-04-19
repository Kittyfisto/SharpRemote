using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum UInt64Enum : ulong
	{
		[EnumMember] A = 0,
		[EnumMember] B = ulong.MaxValue / 2,
		[EnumMember] C = ulong.MaxValue
	}
}