using System;

namespace SharpRemote
{
	public static class DisposableExtensions
	{
		public static void TryDispose(this IDisposable that)
		{
			if (that == null)
				return;

			try
			{
				that.Dispose();
			}
			catch (Exception)
			{}
		}
	}
}