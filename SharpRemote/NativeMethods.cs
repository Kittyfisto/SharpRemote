using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using SharpRemote.Extensions;
using SharpRemote.Hosting;
using log4net;

namespace SharpRemote
{
	internal static class NativeMethods
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		[Flags]
		public enum ErrorModes : uint
		{
// ReSharper disable InconsistentNaming
			SYSTEM_DEFAULT = 0x0,
			SEM_FAILCRITICALERRORS = 0x0001,
			SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
			SEM_NOGPFAULTERRORBOX = 0x0002,
			SEM_NOOPENFILEERRORBOX = 0x8000
// ReSharper restore InconsistentNaming
		}

		private const string PostmortdemDebuggerDll = "SharpRemote.PostmortemDebugger.dll";
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
					string directory = Assembly.GetExecutingAssembly().GetDirectory();
					if (directory != null)
					{
						string path = Environment.GetEnvironmentVariable("PATH");
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

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool SetDllDirectory(string pathName);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenProcess(
			 ProcessAccessFlags processAccess,
			 bool bInheritHandle,
			 int processId
		);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);

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
					int err = Marshal.GetLastWin32Error();
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
		private static extern bool _installPostmortemDebugger(bool suppressErrorWindows,
		                                                    bool interceptUnhandledExceptions,
		                                                    bool handleCrtAsserts,
		                                                    bool handleCrtPurecalls,
		                                                    CRuntimeVersions crtVersions);

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
					int err = Marshal.GetLastWin32Error();
					Log.ErrorFormat("Unable to install the post-mortem debugger for unhandled exceptions: {0}",
									err);
					return false;
				}

				return true;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Unable to install the post-mortem debugger for unhandled exceptions: {0}",
									e);
				return false;
			}
		}

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