using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Interfaces
{
	[ByReference]
	public interface IByReferenceWithBeforeSerializeCallback
	{
		[BeforeSerialize]
		void BeforeSerialize();
	}
}