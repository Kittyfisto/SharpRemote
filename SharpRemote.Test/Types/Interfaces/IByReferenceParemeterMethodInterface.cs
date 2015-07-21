using SharpRemote.Test.Types.Interfaces.NativeTypes;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IByReferenceParemeterMethodInterface
	{
		void AddListener(IVoidMethodStringParameter listener);
		void RemoveListener(IVoidMethodStringParameter listener);
	}

	public interface IByReferenceReturnMethodInterface
	{
		IVoidMethodStringParameter AddListener();
	}
}