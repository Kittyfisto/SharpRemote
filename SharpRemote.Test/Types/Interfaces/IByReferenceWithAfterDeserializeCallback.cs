using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Interfaces
{
	[ByReference]
	public interface IByReferenceWithAfterDeserializeCallback
	{
		[AfterDeserialize]
		void AfterDeserialize();
	}
}