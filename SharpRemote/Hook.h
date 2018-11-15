#pragma once

#include "stdafx.h"
#include "CRuntimeVersions.h"
#include "Logging.h"

void InstallHook(LPVOID originalFunction,
				 LPVOID hookFunction)
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
#else
		
#endif
}

BOOL LoadMethodAndInstallHook(const wchar_t* library, const char* proc, LPVOID hookFunction)
{
	HMODULE crt = LoadLibrary(library);
	if (crt == nullptr)
	{
		auto err = GetLastError();
		LOG4("Unable to load '", library, "', GetLastError()=", err);
		return FALSE;
	}

	LPVOID wassert = GetProcAddress(crt, proc);
	if (wassert == nullptr)
	{
		auto err = GetLastError();
		LOG6("Unable to find '", proc, "' in '", library, "', GetLastError()=", err);
		return FALSE;
	}

	InstallHook(wassert, hookFunction);

	LOG5("Installed hook for '", proc, "' in '", library, "'");
	return TRUE;
}

typedef void (__cdecl * _CRT_WASSERT_HOOK)(const wchar_t*, const wchar_t*, unsigned);
typedef void (__cdecl * _CRT_SET_PURECALL_HANDLER)(_purecall_handler);
typedef unsigned int (__cdecl * _CRT_SET_ABORT_BEHAVIOR_HANDLER)(unsigned int, unsigned int);

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

	return libs;
}

BOOL InterceptCrtAssert(_CRT_WASSERT_HOOK hookFunction, CRuntimeVersions crtVersions)
{
	auto libs = GetCrtLibraries(crtVersions);
	for(auto it = libs.begin(); it != libs.end(); ++it)
	{
		auto library = *it;
		if (LoadMethodAndInstallHook(library, "_wassert", hookFunction) == FALSE)
			return FALSE;
	}
	return TRUE;
}

BOOL InterceptCrtPurecalls(_purecall_handler hookFunction, CRuntimeVersions crtVersions)
{
	auto libs = GetCrtLibraries(crtVersions);
	for(auto it = libs.begin(); it != libs.end(); ++it)
	{
		HMODULE crt = LoadLibrary(*it);
		if (crt != nullptr)
		{
			auto functionName = "_set_purecall_handler";
			_CRT_SET_PURECALL_HANDLER set_purecall = (_CRT_SET_PURECALL_HANDLER)GetProcAddress(crt, functionName);
			if (set_purecall != nullptr)
			{
				set_purecall(hookFunction);
				LOG2("Installed purecall_handler at 0x", hookFunction);
			}
			else
			{
				const auto err = GetLastError();
				LOG6("Unable to install purecall_handler, '", functionName, "' cannot be found in '", *it, "', GetLastError()=", err);

				return FALSE;
			}
		}
		else
		{
			const auto err = GetLastError();
			LOG4("Unable to install purecall_handler, LoadLibrary(\"", *it, "\") returned NULL, GetLastError()=", err);

			return FALSE;
		}
	}

	return TRUE;
}

BOOL SuppressCrtAborts(CRuntimeVersions crtVersions)
{
	auto libs = GetCrtLibraries(crtVersions);
	BOOL success = FALSE;
	for(auto it = libs.begin(); it != libs.end(); ++it)
	{
		HMODULE crt = LoadLibrary(*it);
		if (crt != nullptr)
		{
			_CRT_SET_ABORT_BEHAVIOR_HANDLER set_abort_behavior = (_CRT_SET_ABORT_BEHAVIOR_HANDLER)GetProcAddress(crt, "_set_abort_behavior");
			if (set_abort_behavior != nullptr)
			{
				set_abort_behavior(0, _WRITE_ABORT_MSG);
				success = TRUE;
			}
		}
	}

	return success;
}