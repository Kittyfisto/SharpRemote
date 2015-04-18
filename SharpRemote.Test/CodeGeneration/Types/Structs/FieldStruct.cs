using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Structs
{
	[DataContract]
	public struct FieldStruct
	{
		[DataMember]
		public double A;

		[DataMember]
		public int B;

		[DataMember] public string C;
	}
}