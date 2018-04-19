using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum Int64Enum : long
	{
		[EnumMember] A = long.MinValue,
		[EnumMember] B = 0,
		[EnumMember] C = long.MaxValue
	}
}