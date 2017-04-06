using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct MissingPropertyGetterStruct
	{
		[DataMember] public int Value { set {  } }
	}
}