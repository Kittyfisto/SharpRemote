using System.Threading;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	/// <summary>
	/// Implementation of a remotable interface that will abort the executing while invoking a method
	/// in order to simulate a failure.
	/// </summary>
	public sealed class AbortsThread
		: IVoidMethodNoParameters
	{
		public void Do()
		{
			Thread.CurrentThread.Abort();
		}
	}
}