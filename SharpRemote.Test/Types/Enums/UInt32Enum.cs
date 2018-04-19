using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum UInt32Enum : uint
	{
		[EnumMember] A = 0,
		[EnumMember] B = uint.MaxValue / 2,
		[EnumMember] C = uint.MaxValue
	}
}