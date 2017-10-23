using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class TooManyBeforeDeserializeCallbacks
	{
		[BeforeDeserialize]
		public void BeforeDeserialize1()
		{ }

		[BeforeDeserialize]
		public void BeforeDeserialize2()
		{ }
	}
}