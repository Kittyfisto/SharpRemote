using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public class Tree
		: BaseClass
	{
		[DataMember]
		public byte B;
	}
}