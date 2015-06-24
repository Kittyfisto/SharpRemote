using System;
using System.Reflection;
using log4net;

namespace SharpRemote
{
	public static class DisposableExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public static void TryDispose(this IDisposable that)
		{
			if (that == null)
				return;

			try
			{
				that.Dispose();
			}
			catch (Exception e)
			{
				Log.WarnFormat("Caught exception while disposing '{0}': {1}", that, e);
			}
		}
	}
}