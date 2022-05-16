using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using Microsoft.Win32;

namespace SharpRemote.EndPoints.Sockets
{
	static class SocketSettings
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		// See the following
		// https://docs.microsoft.com/en-us/biztalk/technical-guides/settings-that-can-be-modified-to-improve-network-performance
		// to understand where the default and key comes from...
		private const string TcpIpParameters = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";

		private static PortRange TryGetEphemeralPortRange(string inetVersion, string protocol, ushort defaultStart, ushort defaultCount)
		{
			try
			{
				var start = new Regex("Start Port\\s*:\\s*(\\d+)", RegexOptions.Singleline);
				var count = new Regex("Number of Ports\\s*:\\s*(\\d+)", RegexOptions.Singleline);

				var process = new Process
				{
					StartInfo =
					{
						FileName = "netsh.exe",
						Arguments = string.Format("int {0} show dynamicport {1}", inetVersion, protocol),
						UseShellExecute = false,
						WindowStyle = ProcessWindowStyle.Hidden,
						CreateNoWindow = true,
						RedirectStandardOutput = true,
						RedirectStandardError = true
					}
				};
				process.Start();
				process.WaitForExit();

				string output = process.StandardOutput.ReadToEnd();
				Log.DebugFormat("netsh output:\r\n{0}", output);

				var match = start.Match(output);
				var startPort = ushort.Parse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);

				match = count.Match(output);
				var portCount = ushort.Parse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
				return new PortRange(startPort, portCount);
			}
			catch (Exception e)
			{
				Log.WarnFormat("Unable to retrieve ephemeral port range, assuming default [{0}, {1}): {2}", e,
				               defaultStart,
				               defaultStart + defaultCount);
				return new PortRange(defaultStart, defaultCount);
			}
		}

		public static class IPv4
		{
			public static class Tcp
			{
				public static PortRange EphemeralPortRange
				{
					get
					{
						const ushort defaultStart = 49152;
						const ushort defaultCount = 16384;

						return TryGetEphemeralPortRange("ipv4", "tcp", defaultStart, defaultCount);
					}
				}
			}
		}

		public static class IPv6
		{
			public static class Tcp
			{
				public static PortRange EphemeralPortRange
				{
					get
					{
						const ushort defaultStart = 49152;
						const ushort defaultCount = 16384;

						return TryGetEphemeralPortRange("ipv6", "tcp", defaultStart, defaultCount);
					}
				}
			}
		}

		public static ushort MaxUserPort
		{
			get
			{
				const int defaultValue = 65535;

#if NET6_0
				return defaultValue;
#else
				try
				{
					var value =
						Registry.GetValue(TcpIpParameters,
						                  "TcpTimedWaitDelay", defaultValue);
					var actualValue = Convert.ToUInt16(value);
					return actualValue;
				}
				catch (Exception)
				{
					return defaultValue;
				}
#endif
			}
		}

		public static TimeSpan TcpTimedWaitDelay
		{
			get
			{
				const int defaultValue = 120;

#if NET6_0
				return TimeSpan.FromSeconds(defaultValue);
#else
				try
				{
					var value =
						Registry.GetValue(TcpIpParameters,
						                  "TcpTimedWaitDelay", defaultValue);
					var actualValue = Convert.ToInt32(value);
					return TimeSpan.FromSeconds(actualValue);
				}
				catch (Exception)
				{
					return TimeSpan.FromSeconds(defaultValue);
				}
#endif
			}
		}
	}
}
