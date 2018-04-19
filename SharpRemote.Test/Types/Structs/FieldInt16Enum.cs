using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldInt16Enum
	{
		[DataMember] public Int16Enum Value;
	}
}