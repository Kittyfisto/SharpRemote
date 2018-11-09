using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using log4net;
using SharpRemote.Extensions;
using SharpRemote.Hosting;

namespace SharpRemote
{
	/// <summary>
	///     Provides access to native methods from "SharpRemote.PostmortemDebugger.dll":
	///     Allows interception of various failures (such as access violations, pure virtual
	///     function calls, assertions, etc..) and minidump creation when they occur.
	/// </summary>
	public static class NativeMethods
	{
		/// <summary>
		/// </summary>
		[Flags]
		public enum ErrorModes : uint
		{
// ReSharper disable InconsistentNaming
			/// <summary>
			/// </summary>
			SYSTEM_DEFAULT = 0x0,

			/// <summary>
			/// </summary>
			SEM_FAILCRITICALERRORS = 0x0001,

			/// <summary>
			/// </summary>
			SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,

			/// <summary>
			/// </summary>
			SEM_NOGPFAULTERRORBOX = 0x0002,

			/// <summary>
			/// </summary>
			SEM_NOOPENFILEERRORBOX = 0x8000
// ReSharper restore InconsistentNaming
		}

		private const string PostmortdemDebuggerDll = "SharpRemote.PostmortemDebugger.dll";
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly object SyncRoot = new object();
		private static IntPtr _library;

		/// <summary>
		///     Loads the native post-mortdem debugger DLL, adjusting the search paths, if necessary...
		/// </summary>
		public static bool LoadPostmortemDebugger()
		{
			return LoadLibrary(ref _library, PostmortdemDebuggerDll);
		}

		internal static bool LoadLibrary(ref IntPtr library, string libraryPath)
		{
			lock (SyncRoot)
			{
				if (library != IntPtr.Zero)
					return true;

				library = LoadLibrary(libraryPath);
				if (library == IntPtr.Zero)
				{
					var directory = Assembly.GetExecutingAssembly().GetDirectory();
					if (directory != null)
					{
						var path = Environment.GetEnvironmentVariable("PATH");
						path = string.Format("{0};{1}",
						                     Path.Combine(directory, Environment.Is64BitProcess ? "x64" : "x86"),
						                     path
						                    );
						Environment.SetEnvironmentVariable("PATH", path);

						library = LoadLibrary(libraryPath);
						return library != IntPtr.Zero;
					}

					return false;
				}

				return true;
			}
		}

		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

		/// <summary>
		///     Adds a directory to the search path used to locate DLLs for the application.
		///     <see
		///         href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms686203%28v=vs.85%29.aspx" />
		/// </summary>
		/// <param name="pathName"></param>
		/// <returns></returns>
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetDllDirectory(string pathName);

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

		#region Postmortem debugging

		[DllImport(PostmortdemDebuggerDll,
			SetLastError = true,
			CharSet = CharSet.Unicode,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "InitDumpCollection")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool _initDumpCollection(
			int numRetainedMinidumps,
			string dumpFolder,
			string dumpName
		);

		/// <summary>
		///     Configures the post mortem debugger of the current process
		///     to store minidumps in the given folder.
		///     If there are more than the given amount of minidumps, then the oldest
		///     minidumps are removed upon creation of new dumps.
		/// </summary>
		/// <param name="numRetainedMinidumps"></param>
		/// <param name="dumpFolder"></param>
		/// <param name="dumpName"></param>
		/// <returns></returns>
		public static bool InitDumpCollection(
			int numRetainedMinidumps,
			string dumpFolder,
			string dumpName
		)
		{
			try
			{
				if (!_initDumpCollection(numRetainedMinidumps, dumpFolder, dumpName))
				{
					var err = Marshal.GetLastWin32Error();
					Log.ErrorFormat("Unable to initialize the post-mortem debugger: {0}", err);
					return false;
				}

				return true;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Unable to initialize the post-mortem debugger: {0}", e);
				return false;
			}
		}

		[DllImport(PostmortdemDebuggerDll,
			SetLastError = true,
			CallingConvention = CallingConvention.Cdecl,
			EntryPoint = "InstallPostmortemDebugger")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool _installPostmortemDebugger([MarshalAs(UnmanagedType.Bool)] bool suppressErrorWindows,
		                                                      [MarshalAs(UnmanagedType.Bool)] bool interceptUnhandledExceptions,
		                                                      [MarshalAs(UnmanagedType.Bool)] bool handleCrtAsserts,
		                                                      [MarshalAs(UnmanagedType.Bool)] bool handleCrtPurecalls,
		                                                      CRuntimeVersions crtVersions);

		/// <summary>
		///     Installs a post mortem debugger in the current process which handles the given failures.
		///     Should a failure occur, a minidump is written automatically.
		/// </summary>
		/// <param name="suppressErrorWindows"></param>
		/// <param name="interceptUnhandledExceptions"></param>
		/// <param name="handleCrtAsserts"></param>
		/// <param name="handleCrtPurecalls"></param>
		/// <param name="crtVersions"></param>
		/// <returns></returns>
		public static bool InstallPostmortemDebugger(bool suppressErrorWindows,
		                                             bool interceptUnhandledExceptions,
		                                             bool handleCrtAsserts,
		                                             bool handleCrtPurecalls,
		                                             CRuntimeVersions crtVersions)
		{
			try
			{
				if (!_installPostmortemDebugger(suppressErrorWindows,
				                                interceptUnhandledExceptions,
				                                handleCrtAsserts,
				                                handleCrtPurecalls,
				                                crtVersions))
				{
					var err = Marshal.GetLastWin32Error();
					Log.ErrorFormat("Unable to install the post-mortem debugger for unhandled exceptions: {0}",
					                err);
					return false;
				}

				Log.InfoFormat("Test");

				return true;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Unable to install the post-mortem debugger for unhandled exceptions: {0}",
				                e);
				return false;
			}
		}

		/// <summary>
		///     Creates a minidump of the given process and stores it in the given file.
		/// </summary>
		/// <param name="processId"></param>
		/// <param name="dumpName"></param>
		/// <returns></returns>
		[DllImport(PostmortdemDebuggerDll, SetLastError = true, CharSet = CharSet.Unicode,
			CallingConvention = CallingConvention.Cdecl)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool CreateMiniDump(
			int processId,
			string dumpName
		);

		#endregion
	}
}