using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldInt32Enum
	{
		[DataMember] public Int32Enum Value;
	}
}
