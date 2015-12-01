using System;
using System.IO;
using System.Threading;

namespace SharpRemote
{
	public static class DirectoryInfoExtensions
	{
		public static void TryDelete(this DirectoryInfo file)
		{
			for (int i = 0; i < 10; ++i)
			{
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
}