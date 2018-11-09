using System;
using log4net;

namespace SharpRemote.Extensions
{
	internal static class DisposableExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger("SharpRemote.DisposableExtensions");

		public static void TryDispose(this IDisposable that)
		{
			TryDispose(that, logError: true);
		}

		public static void TryDispose(this IDisposable that, bool logError)
		{
			if (that == null)
				return;

			try
			{
				that.Dispose();
			}
			catch (Exception e)
			{
				if (logError)
				{
					Log.WarnFormat("Caught exception while disposing '{0}': {1}", that, e);
				}
				else
				{
					Log.DebugFormat("Caught exception while disposing '{0}': {1}", that, e);
				}
			}
		}
	}
}