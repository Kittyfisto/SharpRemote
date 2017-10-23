using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Interfaces
{
	[ByReference]
	public interface IByReferenceWithBeforeDeserializeCallback
	{
		[BeforeDeserialize]
		void BeforeDeserialize();
	}
}