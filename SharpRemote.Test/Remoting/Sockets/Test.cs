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
	public class Test
		: AbstractEndPointTestTest
	{
		internal override IInternalRemotingEndPoint CreateClient(string name = null,
		                                                         IAuthenticator clientAuthenticator = null,
		                                                         IAuthenticator serverAuthenticator = null,
		                                                         LatencySettings latencySettings = null,
		                                                         HeartbeatSettings heartbeatSettings = null,
		                                                         NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketEndPoint(EndPointType.Client,
			                          name, clientAuthenticator, serverAuthenticator, networkServiceDiscoverer: null,
			                          latencySettings: latencySettings,
			                          heartbeatSettings: heartbeatSettings);
		}

		internal override IInternalRemotingEndPoint CreateServer(string name = null,
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
			                          serverAuthenticator, networkServiceDiscoverer: null,
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

		protected override void Connect(IRemotingEndPoint rep1, EndPoint localEndPoint)
		{
			((SocketEndPoint) rep1).Connect((IPEndPoint) localEndPoint);
		}

		protected override void Connect(IRemotingEndPoint rep1, EndPoint localEndPoint, TimeSpan timeout)
		{
			((SocketEndPoint) rep1).Connect((IPEndPoint) localEndPoint, timeout);
		}

		protected override EndPoint EndPoint1 => new IPEndPoint(IPAddress.Loopback, port: 56783);

		[Test]
		[Description("Verifies that creating a peer-endpoint without specifying a port works and assigns a free port")]
		public void TestCtor1()
		{
			using (var server = CreateServer())
			{
				Bind(server);

				server.LocalEndPoint.Should().NotBeNull();
				((IPEndPoint) server.LocalEndPoint).Address.Should().Be(IPAddress.Loopback);
				((IPEndPoint) server.LocalEndPoint).Port.Should()
				                                   .BeInRange(minimumValue: 49152, maximumValue: 65535,
				                                              because:
				                                              "because an automatically chosen port should be in the range of private/dynamic port numbers");
				server.RemoteEndPoint.Should().BeNull();
				server.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that disposing the endpoint actually closes the listening socket")]
		public void TestDispose1()
		{
			EndPoint endpoint;
			using (var ep = CreateServer("Foo"))
			{
				Bind(ep);
				endpoint = ep.LocalEndPoint;
			}

			// If the SocketRemotingEndPoint correctly disposed the listening socket, then
			// we should be able to create a new socket on the same address/port.
			using (var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
			{
				new Action(() => socket.Bind(endpoint))
					.ShouldNotThrow("Because the corresponding endpoint should no longer be in use");
			}
		}
	}
}