using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Structs
{
	[DataContract]
	public sealed class FieldSealedClass
	{
		[DataMember] public double A;

		[DataMember] public int B;

		[DataMember] public string C;
	}
}