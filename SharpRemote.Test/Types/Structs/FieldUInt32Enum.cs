using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldUInt32Enum
	{
		[DataMember] public UInt32Enum Value;
	}
}