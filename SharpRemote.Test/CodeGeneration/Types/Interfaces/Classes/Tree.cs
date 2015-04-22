using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Interfaces.Classes
{
	[DataContract]
	public class Tree
		: BaseClass
	{
		[DataMember]
		public byte B;
	}
}