using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldUInt16Enum
	{
		[DataMember] public UInt16Enum Value;
	}
}