using SharpRemote.Test.Types.Interfaces.NativeTypes;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IByReferenceParemeterMethodInterface
	{
		void AddListener([ByReference] IVoidMethodStringParameter listener);
		void RemoveListener([ByReference] IVoidMethodStringParameter listener);
	}
}