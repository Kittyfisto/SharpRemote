using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Structs
{
	[DataContract]
	public class Birke
		: Tree
	{
		[DataMember]
		public string C;
	}
}