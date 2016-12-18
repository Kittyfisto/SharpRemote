using System;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class SocketRemotingEndPointClientTest
	{
		[Test]
		public void TestConnect1()
		{
			var discoverer = new Mock<INetworkServiceDiscoverer>();

			using (var server = new SocketRemotingEndPointClient(networkServiceDiscoverer: discoverer.Object))
			{
				new Action(() => server.Connect("foobar")).ShouldThrow<NoSuchEndPointException>();
				discoverer.Verify(x => x.FindServices(It.Is<string>(name => name == "foobar")),
				                  Times.Once);
			}
		}
	}
}