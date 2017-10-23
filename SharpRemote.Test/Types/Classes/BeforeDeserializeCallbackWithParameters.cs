using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class BeforeDeserializeCallbackWithParameters
	{
		[BeforeDeserialize]
		public void BeforeDeserialize(object foo, int bar)
		{ }
	}
}