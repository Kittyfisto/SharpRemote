using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class TooManyBeforeSerializeCallbacks
	{
		[BeforeSerialize]
		public void BeforeSerialize1()
		{ }

		[BeforeSerialize]
		public void BeforeSerialize2()
		{ }
	}
}