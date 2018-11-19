using System;
using System.Reflection;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;
using SharpRemote.Sockets;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class SocketEndPointClientTest
	{
		[Test]
		public void TestConnect1()
		{
			var discoverer = new Mock<INetworkServiceDiscoverer>();

			using (var client = new SocketEndPoint(EndPointType.Client, networkServiceDiscoverer: discoverer.Object))
			{
				new Action(() => client.Connect("foobar")).ShouldThrow<NoSuchEndPointException>();
				discoverer.Verify(x => x.FindServices(It.Is<string>(name => name == "foobar")),
				                  Times.Once);
			}
		}

		[Test]
		public void TestDispose()
		{
			SocketEndPoint client;
			Heartbeat heartbeat;

			using (client = new SocketEndPoint(EndPointType.Client))
			{
				heartbeat = GetLocalHeartbeat(client);
				heartbeat.IsDisposed.Should().BeFalse("because the heartbeat object should still be in use");
			}

			heartbeat.IsDisposed.Should().BeTrue("because the heartbeat object should've been disposed of");
		}

		private static Heartbeat GetLocalHeartbeat(SocketEndPoint client)
		{
			var field = typeof(AbstractBinaryStreamEndPoint<ISocket>).GetField("_localHeartbeat", BindingFlags.NonPublic | BindingFlags.Instance);
			var value = field.GetValue(client);
			value.Should().BeOfType<Heartbeat>();
			return (Heartbeat) value;
		}
	}
}