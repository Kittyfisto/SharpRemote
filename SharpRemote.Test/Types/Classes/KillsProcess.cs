using System.Diagnostics;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	/// <summary>
	/// Implementation of a remotable interface that will kill the process while invoking a method
	/// in order to simulate a failure.
	/// </summary>
	public sealed class KillsProcess
		: IVoidMethodNoParameters
	{
		public void Do()
		{
			Process.GetCurrentProcess().Kill();
		}
	}
}