using System.IO;
using System.Linq;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Can be used to configure how the post mortem debugger of a silo works,
	/// where dumps are stored, how many, etc..
	/// </summary>
	public sealed class PostMortemSettings
	{
		/// <summary>
		/// Whether or not minidumps shall be collected
		/// </summary>
		/// <remarks>
		/// Is set to false by default.
		/// </remarks>
		public bool CollectMinidumps;

		/// <summary>
		/// Whether or not the "XYZ has stopped working" window that is created by windows
		/// when an application experiences an unhandled exception / access violation is suppressed.
		/// When suppressed the window will not be shown to the user and the process will terminate immediately.
		/// </summary>
		/// <remarks>
		/// When <see cref="CollectMinidumps"/> is set to true then a minidump is collected before termination.
		/// </remarks>
		/// <remarks>
		/// Is set to false by default.
		/// </remarks>
		public bool SupressStoppedWorkingWindow;

		/// <summary>
		/// Whether or not CRT assertion failed window is suppressed.
		/// When suppressed the window will not be shown to the user and the process will terminate immediately.
		/// </summary>
		/// <remarks>
		/// When <see cref="CollectMinidumps"/> is set to true then a minidump is collected before termination.
		/// </remarks>
		/// <remarks>
		/// Is set to false by default.
		/// </remarks>
		public bool SuppresCrtAssertWindow;

		/// <summary>
		/// The maximum amount of minidumps that shall be retained.
		/// Once more are created, the oldest ones are removed.
		/// </summary>
		/// <remarks>
		/// Must be 1 or greater.
		/// </remarks>
		public int NumMinidumpsRetained;

		/// <summary>
		/// The folder where minidumps are to be stored.
		/// This application must have permission to write files to that location.
		/// </summary>
		/// <remarks>
		/// Must be set if <see cref="CollectMinidumps"/> is set to true.
		/// </remarks>
		public string MinidumpFolder;

		/// <summary>
		/// The name of the minidumps.
		/// A minidump is stored as "{MinidumpFolder}{MinidumpName}{current_datetime}.dmp" and
		/// thus the name should be descriptive enough to figure out the application it belongs to.
		/// </summary>
		/// <remarks>
		/// Must be set if <see cref="CollectMinidumps"/> is set to true.
		/// </remarks>
		public string MinidumpName;

		/// <summary>
		/// Tests if the values set are valid.
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

		public override string ToString()
		{
			return string.Format("SupressStoppedWorkingWindow: {0}, CollectMinidumps: {1}, NumMinidumpsRetained: {2}, SuppresCrtAssertWindow: {3}, MinidumpFolder: {4}, MinidumpName: {5}", SupressStoppedWorkingWindow, CollectMinidumps, NumMinidumpsRetained, SuppresCrtAssertWindow, MinidumpFolder, MinidumpName);
		}

		/// <summary>
		/// Creates a clone of this object.
		/// </summary>
		/// <returns></returns>
		public PostMortemSettings Clone()
		{
			return new PostMortemSettings
				{
					CollectMinidumps = CollectMinidumps,
					MinidumpName = MinidumpName,
					MinidumpFolder = MinidumpFolder,
					NumMinidumpsRetained = NumMinidumpsRetained,
					SupressStoppedWorkingWindow = SupressStoppedWorkingWindow,
					SuppresCrtAssertWindow = SuppresCrtAssertWindow
				};
		}
	}
}