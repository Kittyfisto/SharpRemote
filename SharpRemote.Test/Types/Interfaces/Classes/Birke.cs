using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Interfaces.Classes
{
	[DataContract]
	public class Birke
		: Tree
	{
		[DataMember]
		public string C;
	}
}