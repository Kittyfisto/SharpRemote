using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Enums
{
	[DataContract]
	public enum DataContractEnum
	{
		[EnumMember] A,
		[EnumMember] B,
		[EnumMember] C
	}
}