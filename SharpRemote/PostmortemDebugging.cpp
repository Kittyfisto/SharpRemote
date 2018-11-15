#include "stdafx.h"
#include "PostmortemDebugging.h"
#include "Hook.h"
#include "Logging.h"

typedef BOOL (__stdcall *PDUMPFN)(
	HANDLE hProcess,
	DWORD ProcessId,
	HANDLE hFile,
	MINIDUMP_TYPE DumpType,
	PMINIDUMP_EXCEPTION_INFORMATION ExceptionParam,
	PMINIDUMP_USER_STREAM_INFORMATION UserStreamParam,
	PMINIDUMP_CALLBACK_INFORMATION CallbackParam
);

bool _collectDumps = false;
int _numRetainedMinidumps = 0;


std::wstring _dateTimeBuffer;
std::wstring _dumpFolder;
std::wstring _dumpName;
std::wstringstream _minidumpStringBuilder;
std::wstring _minidumpFileName;
std::wstring _minidumpPattern;
std::wstring _tmpPath;
std::wstring _oldestFileFullName;

BOOL CheckDumpNameConstraints(const wchar_t* dumpName)
{
	auto dumpNameLength = wcslen(dumpName);
	if (wcschr(dumpName, '/') != NULL ||
		wcschr(dumpName, '\\') != NULL ||
		wcsstr(dumpName, L"..") != NULL ||
		wcsstr(dumpName, L":") != NULL ||
		wcsstr(dumpName, L"*") != NULL ||
		wcsstr(dumpName, L"?") != NULL ||
		wcsstr(dumpName, L"\"") != NULL ||
		dumpNameLength == 0)
	{
		return FALSE;
	}

	return TRUE;
}

BOOL CreateMinidumpFolder()
{
	SECURITY_ATTRIBUTES attr;
	ZeroMemory(&attr, sizeof(SECURITY_ATTRIBUTES));
	attr.nLength = sizeof(SECURITY_ATTRIBUTES);

	auto start = _dumpFolder.begin();
	auto it = start;
	auto end = _dumpFolder.end();
	while((it = std::find(it, end, '\\')) != end)
	{
		auto folder = std::wstring(start, it);
		if (!(folder.length() == 2 && folder[1] == ':'))
		{
			LOG2("Creating directory: '", folder);

			if (CreateDirectory(folder.c_str(), &attr) == FALSE)
			{
				HRESULT err = GetLastError();
				if (err != ERROR_ALREADY_EXISTS)
				{
					LOG4("CreateDirectory '", folder, "'failed: ", err);
					return FALSE;
				}
			}
		}

		// it points to the current '\\' and thus we want to increment it by 1 so
		// we can find the next occurence of '\\' and not the current one which would
		// create an endless loop ;)
		++it;
	}

	LOG1("Created directory");

	return TRUE;
}

BOOL RemoveOldMinidumps()
{
	WIN32_FIND_DATA fdFile;
	HANDLE hFind = nullptr;

	int numFiles = 0;
	FILETIME oldestFileTime;

	LOG2("Removing old minidumps, pattern: ", _minidumpPattern);

	const wchar_t* folder = _minidumpPattern.c_str();
	if ((hFind = FindFirstFile(folder, &fdFile)) == INVALID_HANDLE_VALUE)
	{
		auto err = GetLastError();
		if (err == ERROR_FILE_NOT_FOUND)
		{
			LOG1("No previous dump files found");
			return TRUE;
		}

		LOG2("FindFirstFile failed: ", err);

		return FALSE;
	}
	else
	{
		do
		{
			if(wcscmp(fdFile.cFileName, L".") != 0 && wcscmp(fdFile.cFileName, L"..") != 0)
			{
				_tmpPath.clear();
				_tmpPath.append(folder);
				_tmpPath.append(fdFile.cFileName);

				if(numFiles == 0)
				{
					oldestFileTime = fdFile.ftLastWriteTime;
					_oldestFileFullName = _tmpPath;
				}
				else if (CompareFileTime(&oldestFileTime, &fdFile.ftLastWriteTime) == 1)
				{
					oldestFileTime = fdFile.ftLastWriteTime;
					_oldestFileFullName = _tmpPath;
				}

				if (numFiles+1 >= _numRetainedMinidumps)
				{
					if (!DeleteFile(_oldestFileFullName.c_str()))
					{
						const auto err = GetLastError();
						LOG2("DeleteFile failed: ", err);
						return FALSE;
					}
				}
				else
				{
					++numFiles;
				}
			}
		} while(FindNextFile(hFind, &fdFile) != FALSE);
	}

	LOG1("Removed old minidumps");
	return TRUE;
}

void CreateMinidumpFileName()
{
	SYSTEMTIME time;
	GetLocalTime(&time);

	std::wstringstream& name = _minidumpStringBuilder;

	name << _dumpFolder;
	name << _dumpName;

	name << '_';

	if (time.wDay < 10)
		name << '0';
	name << time.wDay << '.';

	if (time.wMonth < 10)
		name << '0';
	name << time.wMonth;

	name << '.' << time.wYear;
	name << L" - " << time.wHour << '_' << time.wMinute << '_' << time.wSecond;
	name << L".dmp";
	_minidumpFileName = name.str();

	LOG2("Minidump file name: ", _minidumpFileName);
}

void CreateMiniDump(EXCEPTION_POINTERS* exceptionPointers,
					HANDLE processHandle,
					int processId,
					const wchar_t* dumpName)
{
	LOG1("Creating Mini dump...");

	if (CreateMinidumpFolder() == FALSE)
	{
		return;
	}

	// We shouldn't pe persuaded to give up writing the minidump just because we failed to
	// remove old dumps... writing the new one is much more important...
	if (RemoveOldMinidumps() == FALSE)
	{
		LOG1("Failed to remove old minidumps, ignoring it...");
	}

	LOG1("Creating name...");

	CreateMinidumpFileName();

	HANDLE hFile = CreateFile( _minidumpFileName.c_str(),
		GENERIC_READ | GENERIC_WRITE,
		0,
		NULL,
		CREATE_ALWAYS,
		FILE_ATTRIBUTE_NORMAL,
		NULL);

	if (hFile == nullptr)
	{
		auto err = GetLastError();
		LOG2("CreateFile failed: ", err)
		return;
	}

	HMODULE dbghelp = ::LoadLibrary(L"DbgHelp.dll");
	if (dbghelp == nullptr)
		return;

	PDUMPFN miniDumpWriteDump = (PDUMPFN)GetProcAddress(dbghelp, "MiniDumpWriteDump");
	if (miniDumpWriteDump == nullptr)
		return;

	if( ( hFile != nullptr ) && ( hFile != INVALID_HANDLE_VALUE ) )
	{
		MINIDUMP_EXCEPTION_INFORMATION info;

		info.ThreadId           = GetCurrentThreadId();
		info.ExceptionPointers  = exceptionPointers;
		info.ClientPointers     = TRUE;

		MINIDUMP_TYPE mdt       = MiniDumpNormal;

		BOOL rv = (*miniDumpWriteDump)(
			processHandle,
			processId,
			hFile,
			mdt,
			(exceptionPointers != nullptr) ? &info : nullptr,
			0,
			0);

		if (rv != FALSE)
		{
			LOG1("Minidump saved");
		}
		else
		{
			LOG1("MiniDumpWriteDump returned FALSE, unable to write a minidump");
		}

		CloseHandle( hFile );
	}
}

void CreateMiniDump(EXCEPTION_POINTERS* exceptionPointers)
{
	if (_collectDumps)
	{
		CreateMiniDump(exceptionPointers,
			GetCurrentProcess(),
			GetCurrentProcessId(),
			_dumpName.c_str());
	}
	else
	{
		LOG1("NOT creating a minidump because InitDumpCollection has NOT been called (yet)");
	}
}

void failfast()
{
	LOG1("failfast()");

	abort();
}

LONG WINAPI OnUnhandledException(struct _EXCEPTION_POINTERS *exceptionPointers)
{
	LOG1("Caught unhandled exception");

	CreateMiniDump(exceptionPointers);

	return EXCEPTION_EXECUTE_HANDLER;
}

void __cdecl OnCrtAssert( const wchar_t* message, const wchar_t* file, unsigned lineNumber )
{
	LOG1("Caught assert");

	CreateMiniDump(NULL);
	failfast();
}

void __cdecl OnCrtPurecall()
{
	LOG1("Caught pure virtual function call");

	CreateMiniDump(NULL);
	failfast();
}

#ifdef __cplusplus
void DoSetUnhandledExceptionFilter()
{
	auto previous = SetUnhandledExceptionFilter(OnUnhandledException);
	if (previous == nullptr)
	{
		LOG1("Installed unhandled exception filter for the first time!");
	}
	else if (previous != OnUnhandledException)
	{
		LOG4("Another unhandled exception filter was set! Previous=", previous, ", current=", OnUnhandledException);
	}
}

extern "C" {
#endif

BOOL InitDumpCollection(int numRetainedMinidumps, const wchar_t* dumpFolder, const wchar_t* dumpName)
{
	LOG1("InitDumpCollection");

	if (_collectDumps)
	{
		SetLastError(ERROR_ACCESS_DENIED);
		return FALSE;
	}

	if (numRetainedMinidumps <= 0 || dumpFolder == NULL || dumpName == NULL)
	{
		SetLastError(ERROR_BAD_ARGUMENTS);
		return FALSE;
	}

	auto dumpFolderLength = wcslen(dumpFolder);
	if (wcschr(dumpFolder, '/') != NULL || dumpFolder[dumpFolderLength-1] != '\\' ||
		PathIsRelative(dumpFolder) == TRUE)
	{
		SetLastError(ERROR_BAD_ARGUMENTS);
		return FALSE;
	}

	if (CheckDumpNameConstraints(dumpName) == FALSE)
	{
		SetLastError(ERROR_BAD_ARGUMENTS);
		return FALSE;
	}

	_numRetainedMinidumps = numRetainedMinidumps;
	_dumpFolder = dumpFolder;
	_dumpName = dumpName;

	_minidumpPattern = _dumpFolder;
	_minidumpPattern += _dumpName;
	_minidumpPattern += L"*.dmp";

	_tmpPath.reserve(2048);
	_oldestFileFullName.reserve(2048);

	LOG1("Post-Mortem debugger installed");

	_collectDumps = true;
	return TRUE;
}

void QueryHook()
{
	while(true)
	{
		DoSetUnhandledExceptionFilter();

		Sleep(100);
	}
}

void StartThread()
{
	DoSetUnhandledExceptionFilter();

	std::thread t(QueryHook);
	t.detach();
}

BOOL InstallPostmortemDebugger(BOOL suppressErrorWindows,
							   BOOL handleUnhandledExceptions,
							   BOOL handleCrtAsserts,
							   BOOL handleCrtPurecalls,
							   CRuntimeVersions crtVersions)
{
	std::ostringstream message;
	message << "InstallPostmortemDebugger" << std::endl
		<< "  suppressErrorWindows=" << suppressErrorWindows << std::endl
		<< "  handleUnhandledExceptions=" << handleUnhandledExceptions << std::endl
		<< "  handleCrtAsserts=" << handleCrtAsserts << std::endl
		<< "  handleCrtPurecalls=" << handleCrtPurecalls;
	LOG1(message);

	if (suppressErrorWindows == TRUE)
	{
		LOG1("Suppressing error windows...");
		SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);
		SuppressCrtAborts(crtVersions);
	}
	if (handleUnhandledExceptions == TRUE)
	{
		StartThread();
	}
	if (handleCrtAsserts == TRUE)
	{
		InterceptCrtAssert(&OnCrtAssert, crtVersions);
	}
	if (handleCrtPurecalls == TRUE)
	{
		InterceptCrtPurecalls(&OnCrtPurecall, crtVersions);
	}
	return TRUE;
}

BOOL CreateMiniDump(
		int processId,
		const wchar_t* dumpName
		)
{
	LOG1("CreateMiniDump");

	if (_collectDumps == false)
	{
		SetLastError(ERROR_ACCESS_DENIED);
		return FALSE;
	}

	if (CheckDumpNameConstraints(dumpName) == FALSE)
	{
		SetLastError(ERROR_BAD_ARGUMENTS);
		return FALSE;
	}

	HANDLE hProcess = OpenProcess(
		PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_DUP_HANDLE,
		FALSE,
		processId);
	if (hProcess == NULL)
	{
		LOG1("OpenProcess failed...");
		return FALSE;
	}

	CreateMiniDump(NULL,
		hProcess,
		processId,
		dumpName);

	CloseHandle(hProcess);

	return TRUE;
}

#ifdef __cplusplus
}
#endif