#pragma once

#include "stdafx.h"
#include "CRuntimeVersions.h"

typedef void(__cdecl * _CRT_WASSERT_HOOK)(const wchar_t*, const wchar_t*, unsigned);

void DoSetUnhandledExceptionFilter(LPTOP_LEVEL_EXCEPTION_FILTER callback);
BOOL InterceptCrtAssert(_CRT_WASSERT_HOOK hookFunction, CRuntimeVersions crtVersions);
BOOL InterceptCrtPurecalls(_purecall_handler hookFunction, CRuntimeVersions crtVersions);
BOOL SuppressCrtAbortMessages(CRuntimeVersions crtVersions);
