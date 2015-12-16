using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class DoesNothing
		: IVoidMethod
		, IVoidMethodAsyncAttribute
	{
		public void DoStuff()
		{
			
		}

		public void Do(string message)
		{
			
		}
	}
}