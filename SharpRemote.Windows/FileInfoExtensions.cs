using System;
using System.IO;
using System.Threading;

namespace SharpRemote
{
	/// <summary>
	///     Extension methods for the <see cref="FileInfo" /> class.
	/// </summary>
	public static class FileInfoExtensions
	{
		/// <summary>
		///     Tries to delete the given file.
		///     If deleting fails, multiple subsequent tries to delete the file may be performed.
		/// </summary>
		/// <remarks>
		///     This method may block up to 10 seconds.
		/// </remarks>
		/// <param name="file"></param>
		public static void TryDelete(this FileInfo file)
		{
			for (var i = 0; i < 10; ++i)
				try
				{
					file.Delete();
					break;
				}
				catch (UnauthorizedAccessException)
				{
					Thread.Sleep(100 * i);
				}
		}
	}
}