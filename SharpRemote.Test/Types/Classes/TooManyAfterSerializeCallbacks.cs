using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class TooManyAfterSerializeCallbacks
	{
		[AfterSerialize]
		public void AfterSerialize1()
		{}

		[AfterSerialize]
		public void AfterSerialize2()
		{}
	}
}