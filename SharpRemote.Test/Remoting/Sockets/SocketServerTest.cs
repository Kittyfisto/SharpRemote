using System.Net;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class SocketServerTest
	{
		[Test]
		public void TestConstruction()
		{
			using (var server = CreateServer("Foobar"))
			{
				server.Name.Should().Be("Foobar");
				server.LocalEndPoint.Should().BeNull("because the server isn't bound yet");
				server.Connections.Should().BeEmpty("because nobody is connected to that server just yet");
			}
		}

		[Test]
		public void TestConnectDisconnectOneClient()
		{
			using (var server = CreateServer())
			using (var client = CreateClient())
			{
				var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 54320);
				server.Bind(serverEndPoint);
				server.Connections.Should().BeEmpty("because nobody is connected to that server just yet");

				client.Connect(serverEndPoint);
				client.IsConnected.Should().BeTrue();
				server.Connections.Should().HaveCount(1, "because one connection should've been established");

				client.Disconnect();
				server.Property(x => x.Connections).ShouldEventually().BeEmpty("because the last connection was just disconnected");
			}
		}

		private ISocketEndPoint CreateClient()
		{
			return new SocketEndPoint(EndPointType.Client,
			                          "Client");
		}

		private static ISocketServer CreateServer(string name = "Server")
		{
			return new SocketServer(name);
		}
	}
}