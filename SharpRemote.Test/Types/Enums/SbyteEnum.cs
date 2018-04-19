using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum SbyteEnum : sbyte
	{
		[EnumMember] A = sbyte.MinValue,
		[EnumMember] B = 0,
		[EnumMember] C = sbyte.MaxValue
	}
}