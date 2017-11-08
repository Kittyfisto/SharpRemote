using System;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Defines the list of all supported C-Runtime versions that are supported.
	/// Runtime failures can only be handled for supported versions.
	/// The list of failures that are handled are:
	/// - assert() failures - dialog can be suppressed, a minidump can be created
	/// - abort() calls - dialog can be suppressed, a minidump can be created
	/// - pure virtual function calls
	/// </summary>
	[Flags]
	public enum CRuntimeVersions
	{
		/// <summary>
		/// No CRT version should be supported - the default.
		/// </summary>
		None = 0,

		/// <summary>
		/// CRT version 7.1 (Visual Studio 2003) should be supported.
		/// </summary>
		_71 = 0x00000001,

		/// <summary>
		/// CRT version 8.0 (Visual Studio 2005) should be supported.
		/// </summary>
		_80 = 0x00000002,

		/// <summary>
		/// CRT version 9.0 (Visual Studio 2008) should be supported.
		/// </summary>
		_90 = 0x00000004,

		/// <summary>
		/// CRT version 10.0 (Visual Studio 2010) should be supported.
		/// </summary>
		_100 = 0x00000008,

		/// <summary>
		/// CRT version 11.0 (Visual Studio 2012) should be supported.
		/// </summary>
		_110 = 0x00000010,

		/// <summary>
		/// CRT version 12.0 (Visual Studio 2013) should be supported.
		/// </summary>
		_120 = 0x00000020,

		/// <summary>
		/// Debug version(s) of the CRT should be supported.
		/// </summary>
		Debug = 0x20000000,

		/// <summary>
		/// Release version(s) of the CRT should be supported.
		/// </summary>
		Release = 0x40000000,

		/// <summary>
		/// All possible debug CRT versions should be supported.
		/// </summary>
		AllDebug = _71 | _80 | _90 | _100 | _110 | _120 | Debug,

		/// <summary>
		/// All possible release CRT versions should be supported.
		/// </summary>
		AllRelease = _71 | _80 | _90 | _100 | _110 | _120 | Release,
	};
}