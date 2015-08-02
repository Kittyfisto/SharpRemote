using System.IO;
using System.Linq;
using System.Runtime.Serialization;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Can be used to configure how the post mortem debugger of a silo works,
	/// where dumps are stored, how many, etc..
	/// </summary>
	[DataContract]
	public sealed class PostMortemSettings
	{
		/// <summary>
		/// Whether or not minidumps shall be collected
		/// </summary>
		/// <remarks>
		/// Is set to false by default.
		/// </remarks>
		[DataMember(Order = 1)]
		public bool CollectMinidumps;

		/// <summary>
		/// Whether or not the "XYZ has stopped working" window that is created by windows
		/// when an application experiences an unhandled exception / access violation.
		/// </summary>
		/// <remarks>
		/// Is set to false by default.
		/// </remarks>
		[DataMember(Order = 2)]
		public bool SupressStoppedWorkingWindow;

		/// <summary>
		/// The maximum amount of minidumps that shall be retained.
		/// Once more are created, the oldest ones are removed.
		/// </summary>
		/// <remarks>
		/// Must be 1 or greater.
		/// </remarks>
		[DataMember(Order = 3)]
		public int NumMinidumpsRetained;

		/// <summary>
		/// The folder where minidumps are to be stored.
		/// This application must have permission to write files to that location.
		/// </summary>
		/// <remarks>
		/// Must be set if <see cref="CollectMinidumps"/> is set to true.
		/// </remarks>
		[DataMember(Order = 4)]
		public string MinidumpFolder;

		/// <summary>
		/// The name of the minidumps.
		/// A minidump is stored as "{MinidumpFolder}{MinidumpName}{current_datetime}.dmp" and
		/// thus the name should be descriptive enough to figure out the application it belongs to.
		/// </summary>
		/// <remarks>
		/// Must be set if <see cref="CollectMinidumps"/> is set to true.
		/// </remarks>
		[DataMember(Order = 5)]
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
			return string.Format("CollectMinidumps: {0}, SupressStoppedWorkingWindow: {1}, NumMinidumpsRetained: {2}, MinidumpFolder: {3}, MinidumpName: {4}", CollectMinidumps, SupressStoppedWorkingWindow, NumMinidumpsRetained, MinidumpFolder, MinidumpName);
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
					SupressStoppedWorkingWindow = SupressStoppedWorkingWindow
				};
		}
	}
}