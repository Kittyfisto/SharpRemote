using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct MissingPropertySetterStruct
	{
		[DataMember] public int Value { get { return 42; } }
	}
}