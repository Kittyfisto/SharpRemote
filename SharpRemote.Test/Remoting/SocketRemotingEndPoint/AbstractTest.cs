using System;
using System.Threading;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;
using log4net.Core;

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	public abstract class AbstractTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<AbstractSocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<AbstractIPSocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointClient>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointServer>(Level.Info);
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			TestLogger.DisableConsoleLogging();
		}

		protected SocketRemotingEndPointClient CreateClient(string name = null, IAuthenticator clientAuthenticator = null,
		                                                    IAuthenticator serverAuthenticator = null,
		                                                    NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                                    LatencySettings latencySettings = null)
		{
			return new SocketRemotingEndPointClient(name, clientAuthenticator, serverAuthenticator, null,
			                                        networkServiceDiscoverer,
			                                        latencySettings: latencySettings);
		}

		protected SocketRemotingEndPointServer CreateServer(string name = null, IAuthenticator clientAuthenticator = null,
		                                                    IAuthenticator serverAuthenticator = null,
		                                                    NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                                    LatencySettings latencySettings = null)
		{
			return new SocketRemotingEndPointServer(name,
			                                        clientAuthenticator,
			                                        serverAuthenticator, null,
			                                        networkServiceDiscoverer,
			                                        latencySettings: latencySettings);
		}

		protected static bool WaitFor(Func<bool> fn, TimeSpan timeout)
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