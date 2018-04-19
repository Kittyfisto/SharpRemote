using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldByteEnum
	{
		[DataMember] public ByteEnum Value;
	}
}