using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class BeforeSerializeCallbackWithParameters
	{
		[BeforeSerialize]
		public void BeforeSerialize(object foo)
		{ }
	}
}