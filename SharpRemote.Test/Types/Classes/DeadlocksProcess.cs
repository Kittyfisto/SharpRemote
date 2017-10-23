using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	/// <summary>
	/// This class suspends all threads of the calling process (the executing thread at last ;))
	/// in order to simulate a complete process deadlock.
	/// </summary>
	public sealed class DeadlocksProcess
		: IVoidMethodNoParameters
	{
		private const uint THREAD_SUSPEND_RESUME = 0x0002;

		public void Do()
		{
#pragma warning disable 618
			var currentId = AppDomain.GetCurrentThreadId();
#pragma warning restore 618

			var threads = Process.GetCurrentProcess().Threads
			                     .Cast<ProcessThread>()
			                     .Where(x => x.Id != currentId);

			IntPtr handle;
			foreach (var thread in threads)
			{
				handle = OpenThread(THREAD_SUSPEND_RESUME, false, (uint) thread.Id);
				SuspendThread(handle);
			}

			handle = OpenThread(THREAD_SUSPEND_RESUME, false, (uint) currentId);
			SuspendThread(handle);
		}

		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

		[DllImport("kernel32.dll")]
		private static extern uint SuspendThread(IntPtr hThread);
	}
}