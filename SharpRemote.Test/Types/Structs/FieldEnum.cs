using System.Runtime.Serialization;
using SharpRemote.Test.Types.Enums;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldEnum
	{
		[DataMember] public DataContractEnum Value;
	}
}
