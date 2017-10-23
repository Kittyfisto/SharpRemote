using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class TooManyAfterDeserializeCallbacks
	{
		[AfterDeserialize]
		public void AfterDeserialize1()
		{ }

		[AfterDeserialize]
		public void AfterDeserialize2()
		{ }
	}
}