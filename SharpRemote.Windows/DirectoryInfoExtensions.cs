using System;
using System.IO;
using System.Threading;

namespace SharpRemote
{
	/// <summary>
	///     Provides extension methods for the <see cref="DirectoryInfo" /> class.
	/// </summary>
	public static class DirectoryInfoExtensions
	{
		/// <summary>
		///     Tries to delete the given directory.
		///     Performs multiple tries if deleting fails (up to 10).
		/// </summary>
		/// <remarks>
		///     This method blocks for potentially 10 seconds if deleting fails.
		/// </remarks>
		/// <param name="file"></param>
		public static bool TryDelete(this DirectoryInfo file)
		{
			for (var i = 0; i < 10; ++i)
				try
				{
					file.Delete();
					return true;
				}
				catch (UnauthorizedAccessException)
				{
					Thread.Sleep(100 * i);
				}

			return false;
		}
	}
}