using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Structs
{
	[DataContract]
	public struct ReadOnlyDataMemberFieldStruct
	{
		[DataMember]
		public readonly int Value;
	}
}