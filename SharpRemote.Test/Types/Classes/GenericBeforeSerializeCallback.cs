using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class GenericBeforeSerializeCallback
	{
		[BeforeSerialize]
		public void BeforeSerialize<T>()
		{ }
	}
}