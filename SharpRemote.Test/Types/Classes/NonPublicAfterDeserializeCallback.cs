using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class NonPublicAfterDeserializeCallback
	{
		[AfterDeserialize]
		internal void AfterDeserialize()
		{ }
	}
}