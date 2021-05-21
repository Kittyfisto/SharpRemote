using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class TryConnectTest
		: AbstractTryConnectTest
	{
		internal override IRemotingEndPoint CreateClient(string name = null,
		                                                 IAuthenticator clientAuthenticator = null,
		                                                 IAuthenticator serverAuthenticator = null,
		                                                 LatencySettings latencySettings = null,
		                                                 HeartbeatSettings heartbeatSettings = null,
		                                                 NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketEndPoint(EndPointType.Client,
			                          name,
			                          clientAuthenticator,
			                          serverAuthenticator,
			                          networkServiceDiscoverer,
			                          latencySettings: latencySettings,
			                          heartbeatSettings: heartbeatSettings);
		}

		internal override IRemotingEndPoint CreateServer(string name = null,
		                                                 IAuthenticator clientAuthenticator = null,
		                                                 IAuthenticator serverAuthenticator = null,
		                                                 LatencySettings latencySettings = null,
		                                                 EndPointSettings endPointSettings = null,
		                                                 HeartbeatSettings heartbeatSettings = null,
		                                                 NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketEndPoint(EndPointType.Server,
			                          name,
			                          clientAuthenticator,
			                          serverAuthenticator,
			                          networkServiceDiscoverer,
			                          latencySettings: latencySettings,
			                          endPointSettings: endPointSettings,
			                          heartbeatSettings: heartbeatSettings);
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((ISocketEndPoint) endPoint).Bind(IPAddress.Loopback);
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((ISocketEndPoint) endPoint).Bind((IPEndPoint) address);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout)
		{
			return ((SocketEndPoint) endPoint).TryConnect((IPEndPoint) address, timeout);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, EndPoint address)
		{
			return ((SocketEndPoint) endPoint).TryConnect((IPEndPoint) address);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, string name, TimeSpan timeout)
		{
			return ((SocketEndPoint) endPoint).TryConnect(name, timeout);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, string name)
		{
			return ((SocketEndPoint) endPoint).TryConnect(name);
		}

		protected override void Connect(IRemotingEndPoint endPoint, EndPoint address)
		{
			((SocketEndPoint) endPoint).Connect((IPEndPoint) address);
		}

		protected override EndPoint EndPoint1 => new IPEndPoint(IPAddress.Loopback, port: 58752);

		protected override EndPoint EndPoint2 => new IPEndPoint(IPAddress.Loopback, port: 50012);

		[Test]
		[Description(
			"Verifies that TryConnect() fails when the other socket doesn't respond with the proper greeting message in time")]
		public void TestConnect8()
		{
			using (var rep = CreateClient())
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				socket.Bind(new IPEndPoint(IPAddress.Loopback, port: 54321));
				socket.Listen(backlog: 1);
				socket.BeginAccept(ar => socket.EndAccept(ar), state: null);
				TryConnect(rep, new IPEndPoint(IPAddress.Loopback, port: 54321)).Should().BeFalse();
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this CI server")]
		[Description("Verifies that TryConnect() can establish a connection with an endpoint by specifying its name")]
		public void TestTryConnect23()
		{
			using (var discoverer = new NetworkServiceDiscoverer())
			using (var client = CreateClient("Rep1", networkServiceDiscoverer: discoverer))
			using (var server = CreateServer("Rep2", networkServiceDiscoverer: discoverer))
			{
				Bind(server);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				TryConnect(client, server.Name, TimeSpan.FromSeconds(value: 10))
					.Should().BeTrue();

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() throws when a zero timeout is given")]
		public void TestTryConnect6()
		{
			using (var rep = CreateClient())
			{
				new Action(
				           () => TryConnect(rep, new IPEndPoint(IPAddress.Loopback, port: 12345), TimeSpan.FromSeconds(value: 0)))
					.Should().Throw<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}

		[Test]
		[Description("Verifies that TryConnect() throws when a negative timeout is given")]
		public void TestTryConnect7()
		{
			using (var rep = CreateClient())
			{
				new Action(
				           () => TryConnect(rep, new IPEndPoint(IPAddress.Loopback, port: 12345), TimeSpan.FromSeconds(value: -1)))
					.Should().Throw<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}
	}
}