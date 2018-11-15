#include "stdafx.h"
#include "Hook.h"
#include "Logging.h"

typedef void(__cdecl * _CRT_SET_PURECALL_HANDLER)(_purecall_handler);
typedef unsigned int(__cdecl * _CRT_SET_ABORT_BEHAVIOR_HANDLER)(unsigned int, unsigned int);

void InstallHook(LPVOID originalFunction, LPVOID hookFunction)
{
#ifdef _WIN64

#elif _WIN32
	BYTE cmd[5] = { 0xE9, 0x00, 0x00, 0x00, 0x00 }; //< jmp my_hook
	LPVOID RVAaddr = (LPVOID)((DWORD)originalFunction);
	DWORD offset = (((DWORD)hookFunction) - ((DWORD)originalFunction) - 5);
	memcpy(&cmd[1], &offset, 4);

	DWORD dwProtect;
	BOOL worked;
	DWORD err;

	worked = VirtualProtect(RVAaddr, 5, PAGE_EXECUTE_READWRITE, &dwProtect);
	if (worked == FALSE)
	{
		err = GetLastError();
	}

	worked = WriteProcessMemory(GetCurrentProcess(), RVAaddr, cmd, 5, 0);
	if (worked == FALSE)
	{
		err = GetLastError();
	}

	DWORD oldProtect;
	worked = VirtualProtect(RVAaddr, 5, dwProtect, &oldProtect);
	if (worked == FALSE)
	{
		err = GetLastError();
	}
#endif
}

FARPROC TryGetProcAddress(const wchar_t* library, const char* functionName)
{
	const HMODULE crt = LoadLibrary(library);
	if (crt == nullptr)
	{
		const auto err = GetLastError();
		LOG4("LoadLibrary(\"", library, "\") returned NULL, GetLastError()=", err);
		return nullptr;
	}

	const auto function = GetProcAddress(crt, functionName);
	if (function == nullptr)
	{
		const auto err = GetLastError();
		LOG6("'", functionName, "' cannot be found in '", library, "', GetLastError()=", err);
		return nullptr;
	}

	return function;
}

BOOL LoadMethodAndInstallHook(const wchar_t* library, const char* proc, LPVOID hookFunction)
{
	LPVOID wassert = TryGetProcAddress(library, proc);
	if (wassert == nullptr)
	{
		return FALSE;
	}

	InstallHook(wassert, hookFunction);

	LOG5("Installed hook for '", proc, "' in '", library, "'");
	return TRUE;
}

std::vector<const wchar_t*> GetCrtLibraries(CRuntimeVersions crtVersions)
{
	std::vector<const wchar_t*> libs;

	if ((crtVersions & Crt_71) != 0)
	{
		if ((crtVersions & Crt_Release) != 0)
			libs.push_back(L"MSVCR71.dll");
		if ((crtVersions & Crt_Debug) != 0)
			libs.push_back(L"MSVCR71D.dll");
	}
	if ((crtVersions & Crt_80) != 0)
	{
		if ((crtVersions & Crt_Release) != 0)
			libs.push_back(L"MSVCR80.dll");
		if ((crtVersions & Crt_Debug) != 0)
			libs.push_back(L"MSVCR80D.dll");
	}
	if ((crtVersions & Crt_90) != 0)
	{
		if ((crtVersions & Crt_Release) != 0)
			libs.push_back(L"MSVCR90.dll");
		if ((crtVersions & Crt_Debug) != 0)
			libs.push_back(L"MSVCR90D.dll");
	}
	if ((crtVersions & Crt_100) != 0)
	{
		if ((crtVersions & Crt_Release) != 0)
			libs.push_back(L"MSVCR100.dll");
		if ((crtVersions & Crt_Debug) != 0)
			libs.push_back(L"MSVCR100D.dll");
	}
	if ((crtVersions & Crt_110) != 0)
	{
		if ((crtVersions & Crt_Release) != 0)
			libs.push_back(L"MSVCR110.dll");
		if ((crtVersions & Crt_Debug) != 0)
			libs.push_back(L"MSVCR110D.dll");
	}
	if ((crtVersions & Crt_120) != 0)
	{
		if ((crtVersions & Crt_Release) != 0)
			libs.push_back(L"MSVCR120.dll");
		if ((crtVersions & Crt_Debug) != 0)
			libs.push_back(L"MSVCR120D.dll");
	}
	if ((crtVersions & Crt_140) != 0 || (crtVersions & Crt_141) != 0)
	{
		if ((crtVersions & Crt_Release) != 0)
			libs.push_back(L"ucrtbase.dll");
		if ((crtVersions & Crt_Debug) != 0)
			libs.push_back(L"ucrtbase.dll");
	}

	return libs;
}

void DoSetUnhandledExceptionFilter(LPTOP_LEVEL_EXCEPTION_FILTER callback)
{
	auto previous = SetUnhandledExceptionFilter(callback);
	if (previous == nullptr)
	{
		LOG1("Installed unhandled exception filter...");
	}
	else if (previous != callback)
	{
		LOG4("Installed unhandled exception filter, previous=0x", previous, ", current=0x", callback);
	}
}

BOOL InterceptCrtAssert(_CRT_WASSERT_HOOK hookFunction, CRuntimeVersions crtVersions)
{
	LOG1("Intercepting CRT assert(s)...");

	auto libs = GetCrtLibraries(crtVersions);
	for (auto it = libs.begin(); it != libs.end(); ++it)
	{
		auto library = *it;
		if (LoadMethodAndInstallHook(library, "_wassert", hookFunction) == FALSE)
			return FALSE;
	}

	LOG1("CRT assert(s) will be intercepted and cause a minidump to be created");

	return TRUE;
}

BOOL InterceptCrtPurecalls(_purecall_handler hookFunction, CRuntimeVersions crtVersions)
{
	LOG1("Intercepting CRT pure virtual function calls...");

	auto libs = GetCrtLibraries(crtVersions);
	for (auto it = libs.begin(); it != libs.end(); ++it)
	{
		_CRT_SET_PURECALL_HANDLER set_purecall = (_CRT_SET_PURECALL_HANDLER)TryGetProcAddress(*it, "_set_purecall_handler");
		if (set_purecall == nullptr)
			return FALSE;

		set_purecall(hookFunction);
		LOG2("Installed purecall_handler at 0x", hookFunction);
	}

	LOG1("CRT pure virtual function calls will be intercepted and cause a minidump to be created");

	return TRUE;
}

BOOL SuppressCrtAbortMessages(CRuntimeVersions crtVersions)
{
	LOG1("Suppressing CRT abort messages...");

	SetErrorMode(SEM_FAILCRITICALERRORS | SEM_NOGPFAULTERRORBOX);

	auto libs = GetCrtLibraries(crtVersions);
	for (auto it = libs.begin(); it != libs.end(); ++it)
	{
		_CRT_SET_ABORT_BEHAVIOR_HANDLER set_abort_behavior = (_CRT_SET_ABORT_BEHAVIOR_HANDLER)TryGetProcAddress(*it, "_set_abort_behavior");
		if (set_abort_behavior == nullptr)
			return FALSE;

		set_abort_behavior(0, _WRITE_ABORT_MSG);
	}

	LOG1("CRT abort messages will no longer appear for this process");

	return TRUE;
}