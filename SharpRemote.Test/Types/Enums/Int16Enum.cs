using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum Int16Enum : short
	{
		[EnumMember] A = short.MinValue,
		[EnumMember] B = 0,
		[EnumMember] C = short.MaxValue
	}
}