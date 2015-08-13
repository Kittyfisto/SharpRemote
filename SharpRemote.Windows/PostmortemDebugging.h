#pragma once

#include "CRuntimeVersions.h"




#ifdef __cplusplus
extern "C" {
#endif

	/**
	 * Initializes this port-mortem debugger. Is required to create dumps when an unhandled exception occurs or
	 * to create a mini-dump for another process.
	 *
	 * @param numRetainedMinidumps   
	 * @param dumpFolder             the FULL path where dumps shall be stored; may not contain '/', only '\\' is allowed; must end in '\\'
	 * @param dumpName               the name of the dump to be stored, the final name is <dumpName><datetime>.dmp, so adding .dmp is not neccesary; may not contain '/', '\\' or '..'
	 *
	 * @returns TRUE when the initialization succeeds, FALSE otherwise. Use GetLastError to determine why
	 *
	 * ERROR_ACCESS_DENIED: You called Init() twice when the first call already succeeded
	 */
	__declspec ( dllexport ) BOOL InitDumpCollection(
		int numRetainedMinidumps,
		const wchar_t* dumpFolder,
		const wchar_t* dumpName
		);

	/**
	 * Installs the postmortemdebugger in the calling process which creates a minidump on failure.
	 *
	 * @param suppressErrorWindows        Whether or not "this application has stopped working" and similar windows should be suppressed
	 * @param handleUnhandledExceptions   Whether or not unhandled win32 exceptions (access violations) are intercepted
	 * @param handleCrtAsserts            Whether or not CRT asserts are intercepted
	 * @param handleCrtPurecalls          Whether or not CRT pure virtual function calls are intercepted
	 * @param cRuntimeVersions            The list of CRT versions that should be targeted, if any
	 *
	 * @returns TRUE when the installation succeeds, FALSE otherwise. Use GetLastError to determine why
	 *
	 * ERROR_ACCESS_DENIED: You forgot to call Init() or it returned FALSE
	 */
	__declspec( dllexport ) BOOL InstallPostmortemDebugger(
		BOOL suppressErrorWindows,
		BOOL handleUnhandledExceptions,
		BOOL handleCrtAsserts,
		BOOL handleCrtPurecalls,
		CRuntimeVersions cRuntimeVersions
		);

	/**
	 * Creates a minidump for the given process.
	 *
	 * @param processId   The process id of the process for which to create a minidump
	 * @param dumpName    The overwritten dump name that shall be used as the minidump name, set to NULL if the value given to Init() shall be used
	 *
	 * @returns TRUE when the installation succeeds, FALSE otherwise. Use GetLastError to determine why
	 *
	 * ERROR_ACCESS_DENIED: You forgot to call Init() or it returned FALSE
	 */
	 __declspec( dllexport ) BOOL CreateMiniDump(
		int processId,
		const wchar_t* dumpName
		);

#ifdef __cplusplus
}
#endif