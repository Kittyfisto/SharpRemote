using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum Int32Enum : int
	{
		[EnumMember] A = int.MinValue,
		[EnumMember] B = 0,
		[EnumMember] C = int.MaxValue
	}
}