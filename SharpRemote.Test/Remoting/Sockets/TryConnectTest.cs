using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.EndPoints;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class TryConnectTest
		: AbstractTryConnectTest
	{
		internal override IInternalRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointClient(name, clientAuthenticator, serverAuthenticator, null,
													latencySettings: latencySettings,
													heartbeatSettings: heartbeatSettings);
		}

		internal override IInternalRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, EndPointSettings endPointSettings = null, HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointServer(name,
													clientAuthenticator,
													serverAuthenticator, null,
													latencySettings: latencySettings,
													endPointSettings: endPointSettings,
													heartbeatSettings: heartbeatSettings);
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this CI server")]
		[Description("Verifies that TryConnect() can establish a connection with an endpoint by specifying its name")]
		public void TestTryConnect23()
		{
			using (var discoverer = new NetworkServiceDiscoverer())
			using (var client = CreateClient(name: "Rep1"))
			using (var server = CreateServer(name: "Rep2"))
			{
				Bind(server);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				TryConnect(client, server.Name, TimeSpan.FromSeconds(10))
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
					() => TryConnect(rep, new IPEndPoint(IPAddress.Loopback, 12345), TimeSpan.FromSeconds(0)))
					.ShouldThrow<ArgumentOutOfRangeException>()
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
					() => TryConnect(rep, new IPEndPoint(IPAddress.Loopback, 12345), TimeSpan.FromSeconds(-1)))
					.ShouldThrow<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}

		[Test]
		[Description(
			"Verifies that TryConnect() fails when the other socket doesn't respond with the proper greeting message in time")]
		public void TestConnect8()
		{
			using (var rep = CreateClient())
			using (var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				socket.Bind(new IPEndPoint(IPAddress.Loopback, 54321));
				socket.Listen(1);
				socket.BeginAccept(ar => socket.EndAccept(ar), null);
				TryConnect(rep, new IPEndPoint(IPAddress.Loopback, 54321)).Should().BeFalse();
			}
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((SocketRemotingEndPointServer)endPoint).Bind(IPAddress.Loopback);
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((SocketRemotingEndPointServer)endPoint).Bind((IPEndPoint) address);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout)
		{
			return ((SocketRemotingEndPointClient) endPoint).TryConnect((IPEndPoint) address, timeout);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, EndPoint address)
		{
			return ((SocketRemotingEndPointClient) endPoint).TryConnect((IPEndPoint) address);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, string name, TimeSpan timeout)
		{
			return ((SocketRemotingEndPointClient) endPoint).TryConnect(name, timeout);
		}

		protected override bool TryConnect(IRemotingEndPoint endPoint, string name)
		{
			return ((SocketRemotingEndPointClient) endPoint).TryConnect(name);
		}

		protected override void Connect(IRemotingEndPoint endPoint, EndPoint address)
		{
			((SocketRemotingEndPointClient) endPoint).Connect((IPEndPoint) address);
		}

		protected override EndPoint EndPoint1
		{
			get { return new IPEndPoint(IPAddress.Loopback, 58752); }
		}

		protected override EndPoint EndPoint2
		{
			get { return new IPEndPoint(IPAddress.Loopback, 50012); }
		}
	}
}