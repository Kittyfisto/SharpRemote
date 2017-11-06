using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class RecursiveClass
	{
		[DataMember] public RecursiveClass Left;

		[DataMember] public RecursiveClass Right;
	}
}