using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct ReadOnlyDataMemberFieldStruct
	{
		[DataMember] public readonly int Value;
	}
}