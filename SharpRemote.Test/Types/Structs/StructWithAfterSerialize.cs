using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct StructWithAfterSerialize
	{
		[AfterSerialize]
		public void AfterSerialize()
		{

		}
	}
}