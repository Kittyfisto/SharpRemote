using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldInt64Enum
	{
		[DataMember] public Int64Enum Value;
	}
}