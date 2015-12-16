using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IVoidMethodAsyncInvokeSerialAttribute
	{
		[AsyncRemote]
		[Invoke(Dispatch.SerializePerMethod)]
		void Do(string message);
	}
}