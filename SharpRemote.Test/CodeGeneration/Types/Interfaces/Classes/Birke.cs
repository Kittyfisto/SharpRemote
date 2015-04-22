using System.Runtime.Serialization;

namespace SharpRemote.Test.CodeGeneration.Types.Interfaces.Classes
{
	[DataContract]
	public class Birke
		: Tree
	{
		[DataMember]
		public string C;
	}
}