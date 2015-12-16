using System;
using System.Threading;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class BlocksABit
		: IVoidMethodAsyncInvokeSerialAttribute
	{
		public void Do(string message)
		{
			Thread.Sleep(TimeSpan.FromMilliseconds(1));
		}
	}
}