using System;

namespace SharpRemote
{
	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum ProcessAccessFlags : uint
	{
		/// <summary>
		/// 
		/// </summary>
		All = 0x001F0FFF,

		/// <summary>
		/// Required to terminate a process using TerminateProcess.
		/// </summary>
		Terminate = 0x00000001,

		/// <summary>
		/// 
		/// </summary>
		CreateThread = 0x00000002,

		/// <summary>
		/// 
		/// </summary>
		VirtualMemoryOperation = 0x00000008,

		/// <summary>
		/// 
		/// </summary>
		VirtualMemoryRead = 0x00000010,

		/// <summary>
		/// 
		/// </summary>
		VirtualMemoryWrite = 0x00000020,

		/// <summary>
		/// 
		/// </summary>
		DuplicateHandle = 0x00000040,

		/// <summary>
		/// 
		/// </summary>
		CreateProcess = 0x000000080,

		/// <summary>
		/// 
		/// </summary>
		SetQuota = 0x00000100,

		/// <summary>
		/// 
		/// </summary>
		SetInformation = 0x00000200,

		/// <summary>
		/// 
		/// </summary>
		QueryInformation = 0x00000400,

		/// <summary>
		/// 
		/// </summary>
		QueryLimitedInformation = 0x00001000,

		/// <summary>
		/// 
		/// </summary>
		Synchronize = 0x00100000
	}
}