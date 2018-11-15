using System.IO;
using System.Linq;

namespace SharpRemote.Hosting
{
	/// <summary>
	///     Can be used to configure how the post mortem debugger of a silo works,
	///     where dumps are stored, how many, etc..
	/// </summary>
	public sealed class PostMortemSettings
	{
		/// <summary>
		///     Whether or not minidumps shall be collected
		/// </summary>
		/// <remarks>
		///     Is set to false by default.
		/// </remarks>
		public bool CollectMinidumps;

		/// <summary>
		///     Whether or not (otherwise unhandled) access violations are handled.
		/// </summary>
		/// <remarks>
		///     When <see cref="CollectMinidumps" /> is set to true then a minidump is collected before termination.
		/// </remarks>
		/// <remarks>
		///     Is set to false by default.
		/// </remarks>
		public bool HandleAccessViolations;

		/// <summary>
		///     Whether or not the CRT assertions are handled.
		/// </summary>
		/// <remarks>
		///     When <see cref="CollectMinidumps" /> is set to true then a minidump is collected before termination.
		/// </remarks>
		/// <remarks>
		///     Is set to false by default.
		/// </remarks>
		public bool HandleCrtAsserts;

		/// <summary>
		///     Whether or not the CRT pure virtual function calls are handled.
		/// </summary>
		/// <remarks>
		///     When <see cref="CollectMinidumps" /> is set to true then a minidump is collected before termination.
		/// </remarks>
		/// <remarks>
		///     Is set to false by default.
		/// </remarks>
		public bool HandleCrtPureVirtualFunctionCalls;

		/// <summary>
		///     The folder where minidumps are to be stored.
		///     This application must have permission to write files to that location.
		/// </summary>
		/// <remarks>
		///     Must be set if <see cref="CollectMinidumps" /> is set to true.
		/// </remarks>
		public string MinidumpFolder;

		/// <summary>
		///     The name of the minidumps.
		///     A minidump is stored as "{MinidumpFolder}{MinidumpName}{current_datetime}.dmp" and
		///     thus the name should be descriptive enough to figure out the application it belongs to.
		/// </summary>
		/// <remarks>
		///     Must be set if <see cref="CollectMinidumps" /> is set to true.
		/// </remarks>
		public string MinidumpName;

		/// <summary>
		///     The file path to the logfile which the post mortem debugger shall log to.
		///     When set to a non-null value, the posrt mortem debugger will try to log its
		///     actions (and failures) to the given file.
		/// </summary>
		/// <remarks>
		///     Is set to NULL by default, i.e. no log is written.
		/// </remarks>
		public string LogFileName;

		/// <summary>
		///     The maximum amount of minidumps that shall be retained.
		///     Once more are created, the oldest ones are removed.
		/// </summary>
		/// <remarks>
		///     Must be 1 or greater.
		/// </remarks>
		public int NumMinidumpsRetained;

		/// <summary>
		///     Which (if any) CRT versions should be intercepted for assert, abort, pure virtual function calls, etc..
		/// </summary>
		public CRuntimeVersions RuntimeVersions;

		/// <summary>
		///     Whether or not the:
		///     - "XYZ has stopped working"
		///     - "Assertion failure"
		///     and several other error windows are shown to the user.
		/// </summary>
		/// <remarks>
		///     When <see cref="CollectMinidumps" /> is set to true then a minidump is collected before termination.
		/// </remarks>
		/// <remarks>
		///     Is set to false by default.
		/// </remarks>
		public bool SuppressErrorWindows;

		/// <summary>
		///     Tests if the values set are valid.
		/// </summary>
		public bool IsValid
		{
			get
			{
				if (CollectMinidumps)
				{
					if (string.IsNullOrWhiteSpace(MinidumpFolder))
						return false;

					if (!Path.IsPathRooted(MinidumpFolder))
						return false;

					if (string.IsNullOrWhiteSpace(MinidumpName))
						return false;

					var invalidChars = new[]
						{
							"/",
							"\\",
							"..",
							":",
							"*",
							"?",
							"\""
						};
					if (invalidChars.Any(MinidumpName.Contains))
						return false;

					if (NumMinidumpsRetained <= 0)
						return false;
				}

				return true;
			}
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return
				string.Format(
					"CollectMinidumps: {0}, HandleAccessViolations: {1}, HandleCrtAsserts: {2}, HandleCrtPureVirtualFunctionCalls: {3}, MinidumpFolder: {4}, MinidumpName: {5}, LogFileName: {6}, NumMinidumpsRetained: {7}, RuntimeVersions: {8}, SuppressErrorWindows: {9}",
					CollectMinidumps, HandleAccessViolations, HandleCrtAsserts, HandleCrtPureVirtualFunctionCalls, MinidumpFolder,
					MinidumpName, LogFileName, NumMinidumpsRetained, RuntimeVersions, SuppressErrorWindows);
		}

		/// <summary>
		///     Creates a clone of this object.
		/// </summary>
		/// <returns></returns>
		public PostMortemSettings Clone()
		{
			return new PostMortemSettings
				{
					CollectMinidumps = CollectMinidumps,
					MinidumpName = MinidumpName,
					MinidumpFolder = MinidumpFolder,
					LogFileName = LogFileName,
					NumMinidumpsRetained = NumMinidumpsRetained,
					SuppressErrorWindows = SuppressErrorWindows,
					HandleCrtAsserts = HandleCrtAsserts,
					HandleAccessViolations = HandleAccessViolations,
					HandleCrtPureVirtualFunctionCalls = HandleCrtPureVirtualFunctionCalls,
					RuntimeVersions = RuntimeVersions
				};
		}
	}
}