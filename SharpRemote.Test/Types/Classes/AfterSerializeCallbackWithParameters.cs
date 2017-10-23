using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class AfterSerializeCallbackWithParameters
	{
		[AfterSerialize]
		public void AfterSerialize(int foo)
		{}
	}
}