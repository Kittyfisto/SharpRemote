using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharpRemote.Extensions
{
	internal static class ProcessExtensions
	{
		public static bool TryKill(int pid)
		{
			IntPtr handle = IntPtr.Zero;
			try
			{
				handle = NativeMethods.OpenProcess(ProcessAccessFlags.Terminate,
				                                   false,
				                                   pid);
				if (handle == IntPtr.Zero)
				{
					var err = Marshal.GetLastWin32Error();
					return false;
				}

				if (!NativeMethods.TerminateProcess(handle, 0))
				{
					var err = Marshal.GetLastWin32Error();
					return false;
				}

				return true;
			}
			finally
			{
				NativeMethods.CloseHandle(handle);
			}
		}

		/// <summary>
		/// Tries to kill the given process.
		/// </summary>
		/// <param name="that"></param>
		/// <returns>True when the given process has been killed or doesn't live anymore, false otherwise</returns>
		public static bool TryKill(this Process that)
		{
			if (that == null)
				return true;

			try
			{
				return TryKill(that.Id);
			}
			catch(InvalidOperationException)
			{
				// Process.Id obviously throws an exception when the process doesn't exist anymore.
				return true;
			}
			catch(Exception)
			{
				return false;
			}
		}
	}
}
