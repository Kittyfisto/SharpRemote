using System.Net;
using NUnit.Framework;
using log4net.Core;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public sealed class SocketEndPointTest
		: EndPointTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<SocketEndPoint>(Level.Info);
		}

		protected override IRemotingEndPoint CreateEndPoint(IPAddress address, string name = null)
		{
			return new SocketEndPoint(address, name);
		}
	}
}