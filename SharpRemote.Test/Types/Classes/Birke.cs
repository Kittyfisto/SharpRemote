using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public class Birke
		: Tree
	{
		[DataMember]
		public string C;
	}
}