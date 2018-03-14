using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

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
				server.Bind(IPAddress.Loopback);
				var serverEndPoint = server.LocalEndPoint;
				server.Connections.Should().BeEmpty("because nobody is connected to that server just yet");

				client.Connect(serverEndPoint);
				client.IsConnected.Should().BeTrue();
				server.Property(x => x.Connections).ShouldEventually().HaveCount(1, "because one connection should've been established");

				client.Disconnect();
				server.Property(x => x.Connections).ShouldEventually().BeEmpty("because the last connection was just disconnected");
			}
		}

		[Test]
		public void TestConnectTwoClients()
		{
			using (var server = CreateServer())
			using (var client1 = CreateClient())
			using (var client2 = CreateClient())
			{
				const ulong objectId = 42;

				var subject = new Mock<IGetInt32Property>();
				subject.Setup(x => x.Value).Returns(1337);
				server.RegisterSubject(objectId, subject.Object);

				server.Bind(IPAddress.Loopback);
				var serverEndPoint = server.LocalEndPoint;
				server.Connections.Should().BeEmpty("because nobody is connected to that server just yet");

				client1.Connect(serverEndPoint);
				client1.IsConnected.Should().BeTrue();
				server.Property(x => x.Connections).ShouldEventually().HaveCount(1, "because one connection should've been established");

				client2.Connect(serverEndPoint);
				client2.IsConnected.Should().BeTrue();
				server.Property(x => x.Connections).ShouldEventually().HaveCount(2, "because we've established a 2nd connection");

				client1.CreateProxy<IGetInt32Property>(42).Value.Should().Be(1337);
				subject.Verify(x => x.Value, Times.Once);

				client2.CreateProxy<IGetInt32Property>(42).Value.Should().Be(1337);
				subject.Verify(x => x.Value, Times.Exactly(2));

				client1.Disconnect();
				client2.Disconnect();
				server.Property(x => x.Connections).ShouldEventually().BeEmpty("because we've closed both connections");
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