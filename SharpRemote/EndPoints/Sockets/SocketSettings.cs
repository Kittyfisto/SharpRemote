using System;
using Microsoft.Win32;

namespace SharpRemote.EndPoints.Sockets
{
	static class SocketSettings
	{
		public static TimeSpan TcpTimedWaitDelay
		{
			get
			{
				// See the following
				// https://docs.microsoft.com/en-us/biztalk/technical-guides/settings-that-can-be-modified-to-improve-network-performance
				// to understand where the default and key comes from...
				const int defaultValue = 120;

				try
				{
					var value =
						Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters",
						                  "TcpTimedWaitDelay", defaultValue);
					var actualValue = Convert.ToInt32(value);
					return TimeSpan.FromSeconds(actualValue);
				}
				catch (Exception)
				{
					return TimeSpan.FromSeconds(defaultValue);
				}
			}
		}
	}
}
