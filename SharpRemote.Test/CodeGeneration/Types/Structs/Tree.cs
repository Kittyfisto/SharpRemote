using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Structs
{
	[DataContract]
	public class Tree
		: BaseClass
	{
		[DataMember]
		public byte B;
	}
}