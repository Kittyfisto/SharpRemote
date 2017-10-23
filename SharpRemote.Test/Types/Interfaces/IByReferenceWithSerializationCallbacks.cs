using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Interfaces
{
	[ByReference]
	public interface IByReferenceWithAfterSerializeCallback
	{
		[AfterSerialize]
		void AfterSerialize();
	}
}
