using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum UInt16Enum : ushort
	{
		[EnumMember] A = 0,
		[EnumMember] B = ushort.MaxValue / 2,
		[EnumMember] C = ushort.MaxValue
	}
}