using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using SharpRemote.Extensions;

namespace SharpRemote
{
	/// <summary>
	///     Provides access to native methods from "SharpRemote.PostmortemDebugger.dll":
	///     Allows interception of various failures (such as access violations, pure virtual
	///     function calls, assertions, etc..) and minidump creation when they occur.
	/// </summary>
	public static class NativeMethods
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly object SyncRoot = new object();
		
		/// <summary>
		///     Opens an existing local process object.
		///     <see
		///         href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms684320%28v=vs.85%29.aspx" />
		///     .
		/// </summary>
		/// <param name="processAccess"></param>
		/// <param name="bInheritHandle"></param>
		/// <param name="processId"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenProcess(
			ProcessAccessFlags processAccess,
			bool bInheritHandle,
			int processId
		);

		/// <summary>
		///     Terminates the specified process and all of its threads.
		///     <see
		///         href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms686714%28v=vs.85%29.aspx" />
		/// </summary>
		/// <param name="hProcess"></param>
		/// <param name="uExitCode"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

		/// <summary>
		///     Closes an open object handle.
		///     <see
		///         href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms724211%28v=vs.85%29.aspx" />
		/// </summary>
		/// <param name="hObject"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CloseHandle(IntPtr hObject);
	}
}