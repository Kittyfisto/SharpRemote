using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Structs
{
	[DataContract]
	public struct StaticDataMemberFieldStruct
	{
		[DataMember]
		public static int Value;
	}
}