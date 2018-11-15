#pragma once

/**
 * Defines the list of all supported C-Runtime versions that are supported.
 * Runtime failures can only be handled for supported versions.
 * The list of failures that are handled are:
 * - assert() failures - dialog can be suppressed, a minidump can be created
 * - abort() calls - dialog can be suppressed, a minidump can be created
 * - pure virtual function calls
 */
enum CRuntimeVersions : int
{
	Crt_None    = 0,

	Crt_71     = 0x00000001,
	Crt_80     = 0x00000002,
	Crt_90     = 0x00000004,
	Crt_100    = 0x00000008,
	Crt_110    = 0x00000010,
	Crt_120    = 0x00000020,
	Crt_140    = 0x00000040,
	Crt_141    = 0x00000080,

	Crt_Debug   = 0x20000000,
	Crt_Release = 0x40000000,

	Crt_All_Versions = Crt_71 | Crt_80 | Crt_90 | Crt_100 | Crt_110 | Crt_120,
	Crt_All_Debug = Crt_All_Versions | Crt_Debug,
	Crt_All_Release = Crt_All_Versions | Crt_Release,
};