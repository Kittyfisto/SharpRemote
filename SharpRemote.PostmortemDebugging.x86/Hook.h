#pragma once

#include "stdafx.h"




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
	if (crt != NULL)
	{
		LPVOID wassert = GetProcAddress(crt, "_wassert");
		if (wassert != NULL)
		{
			InstallHook(wassert, hookFunction);
			return TRUE;
		}
	}

	return FALSE;
}

typedef void (__cdecl * _CRT_WASSERT_HOOK)(const wchar_t*, const wchar_t*, unsigned);

std::vector<const wchar_t*> GetCrtLibraries()
{
	std::vector<const wchar_t*> libs;
	libs.push_back(L"MSVCR110D.dll");
	libs.push_back(L"MSVCR110.dll");
	return libs;
}

BOOL InterceptCrtAssert(_CRT_WASSERT_HOOK hookFunction)
{
	auto libs = GetCrtLibraries();
	for(auto it = libs.begin(); it != libs.end(); ++it)
	{
		LoadMethodAndInstallHook(*it, "_wassert", hookFunction);
	}
	return TRUE;
}
