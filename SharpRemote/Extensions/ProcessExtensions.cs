using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;

namespace SharpRemote.Extensions
{
	internal static class ProcessExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static int? TryGetExitCode(this Process process)
		{
			try
			{
				return process.ExitCode;
			}
			catch (Exception e)
			{
				Log.DebugFormat("Caught exception: {0}", e);
				return null;
			}
		}

		public static DateTime? TryGetExitTime(this Process process)
		{
			try
			{
				return process.ExitTime;
			}
			catch (Exception e)
			{
				Log.DebugFormat("Caught exception: {0}", e);
				return null;
			}
		}

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
