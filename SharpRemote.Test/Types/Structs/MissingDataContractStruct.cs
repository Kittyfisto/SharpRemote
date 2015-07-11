using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	public struct MissingDataContractStruct
	{
		[DataMember] public int Value;
	}
}