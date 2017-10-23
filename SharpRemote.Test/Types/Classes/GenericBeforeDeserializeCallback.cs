using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class GenericBeforeDeserializeCallback
	{
		[BeforeDeserialize]
		public void BeforeDeserialize<T>()
		{ }
	}
}