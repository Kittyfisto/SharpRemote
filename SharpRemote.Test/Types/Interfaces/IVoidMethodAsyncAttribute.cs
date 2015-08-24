using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IVoidMethodAsyncAttribute
	{
		[AsyncRemote]
		void Do(string message);
	}
}