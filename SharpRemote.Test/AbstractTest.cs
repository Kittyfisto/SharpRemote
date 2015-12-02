using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;
using log4net.Core;

namespace SharpRemote.Test
{
	public abstract class AbstractTest
	{
		[TestFixtureSetUp]
		public virtual void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			var loggers = Loggers;
			if (loggers != null)
			{
				foreach (var pair in loggers)
				{
					TestLogger.SetLevel(pair.Type, pair.Level);
				}
			}
		}

		/// <summary>
		/// The loggers that shall be enabled for this test and write to the console.
		/// </summary>
		public virtual LogItem[] Loggers { get { return null; } }

		[SetUp]
		public void SetUp()
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			TestLogger.DisableConsoleLogging();
		}

		protected SocketRemotingEndPointClient CreateClient(string name = null, IAuthenticator clientAuthenticator = null,
		                                                    IAuthenticator serverAuthenticator = null,
		                                                    NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                                    LatencySettings latencySettings = null,
		                                                    HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointClient(name, clientAuthenticator, serverAuthenticator, null,
			                                        networkServiceDiscoverer,
			                                        latencySettings: latencySettings,
			                                        heartbeatSettings: heartbeatSettings);
		}

		protected SocketRemotingEndPointServer CreateServer(string name = null,
		                                                    IAuthenticator clientAuthenticator = null,
		                                                    IAuthenticator serverAuthenticator = null,
		                                                    NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                                    LatencySettings latencySettings = null,
		                                                    EndPointSettings endPointSettings = null,
		                                                    HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointServer(name,
			                                        clientAuthenticator,
			                                        serverAuthenticator, null,
			                                        networkServiceDiscoverer,
			                                        latencySettings: latencySettings,
			                                        endPointSettings: endPointSettings,
			                                        heartbeatSettings: heartbeatSettings);
		}

		public static bool WaitFor(Func<bool> fn, TimeSpan timeout)
		{
			DateTime start = DateTime.Now;
			DateTime now = start;
			while ((now - start) < timeout)
			{
				if (fn())
					return true;

				Thread.Sleep(TimeSpan.FromMilliseconds(10));

				now = DateTime.Now;
			}

			return false;
		}
	}
}