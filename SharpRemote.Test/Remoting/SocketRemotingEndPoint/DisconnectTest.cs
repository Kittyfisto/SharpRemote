using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	[TestFixture]
	public sealed class DisconnectTest
		: AbstractTest
	{
		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect1()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);

				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				// Disconnecting from the endpoint that established the connection in the first place
				client.Disconnect();

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep2 is disconnected as well...
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect2()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				// Disconnecting from the other endpoint
				server.Disconnect();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep1 is disconnected as well...
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that disconnecting and connecting to the same endpoint again is possible")]
		public void TestDisconnect3()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));
				client.IsConnected.Should().BeTrue();
				server.IsConnected.Should().BeTrue();

				client.Disconnect();
				client.IsConnected.Should().BeFalse();
				WaitFor(() => !server.IsConnected, TimeSpan.FromSeconds(2))
					.Should().BeTrue();

				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(1)))
					.ShouldNotThrow();
				client.IsConnected.Should().BeTrue();
				server.IsConnected.Should().BeTrue();
			}
		}


		[Test]
		[Description("Verifies that the OnDisconnected event is fired when the connection is disconnected for both the client and the server")]
		public void TestDisconnect4()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				var clients = new List<EndPoint>();
				var servers = new List<EndPoint>();
				client.OnDisconnected += clients.Add;
				server.OnDisconnected += servers.Add;

				var clientEp = client.LocalEndPoint;
				var serverEp = server.LocalEndPoint;

				client.Disconnect();

				WaitFor(() => !server.IsConnected, TimeSpan.FromSeconds(2))
					.Should().BeTrue();

				clients.Should().Equal(serverEp);
				servers.Should().Equal(clientEp);
			}
		}
	}
}