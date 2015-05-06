using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct StaticDataMemberFieldStruct
	{
		[DataMember]
		public static int Value;
	}
}