using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldUInt64Enum
	{
		[DataMember] public UInt64Enum Value;
	}
}