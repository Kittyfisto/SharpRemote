// SharpRemote.Test.Native.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"
#include "SharpRemote.Test.Native.h"

typedef void (__cdecl * _CRT_WASSERT_HOOK)(const wchar_t*, const wchar_t*, unsigned);

void __cdecl my_wassert(_In_z_ const wchar_t * _Message, _In_z_ const wchar_t *_File, _In_ unsigned _Line)
{
	auto crt = LoadLibrary(L"MSVCR110.dll");
	_CRT_WASSERT_HOOK wassert = (_CRT_WASSERT_HOOK)GetProcAddress(crt, "_wassert");
	wassert(_Message, _File, _Line); 
}

#define crt_assert(_Expression) (void)(my_wassert(_CRT_WIDE(#_Expression), _CRT_WIDE(__FILE__), __LINE__), 0)

#ifdef __cplusplus
extern "C" {
#endif

	void produces_assert()
	{
		crt_assert(false != false);
	}

#ifdef __cplusplus
}
#endif
