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
	public sealed class ConnectTest
		: AbstractConnectTest
	{
		public override LogItem[] Loggers
		{
			get
			{
				return new[]
					{
						new LogItem(typeof (SocketRemotingEndPointClient)),
						new LogItem(typeof (SocketRemotingEndPointServer))
					};
			}
		}

		internal override IInternalRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketRemotingEndPointClient(name, clientAuthenticator, serverAuthenticator, null,
													latencySettings: latencySettings,
													heartbeatSettings: heartbeatSettings,
													networkServiceDiscoverer: networkServiceDiscoverer);
		}

		internal override IInternalRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, EndPointSettings endPointSettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketRemotingEndPointServer(name,
													clientAuthenticator,
													serverAuthenticator, null,
													latencySettings: latencySettings,
													endPointSettings: endPointSettings,
													heartbeatSettings: heartbeatSettings,
													networkServiceDiscoverer: networkServiceDiscoverer);
		}

		[Test]
		[Ignore("Why doesn't this work on AppVeyor - I Just don't get it...")]
		public void TestConnect28()
		{
			for (int i = 0; i < 1000; ++i)
			{
				const AddressFamily family = AddressFamily.InterNetwork;
				const SocketType socket = SocketType.Stream;
				const ProtocolType protocol = ProtocolType.Tcp;
				using (var client = new Socket(family, socket, protocol))
				using (var server = new Socket(family, socket, protocol))
				{
					client.ReceiveTimeout = 10000;

					server.ExclusiveAddressUse = true;
					server.Bind(new IPEndPoint(IPAddress.Loopback, 60310));

					bool isConnected = false;
					server.Listen(1);
					server.BeginAccept(ar =>
					{
						Console.WriteLine("BeginAccept handler");
						var serverCon = server.EndAccept(ar);
						Console.WriteLine("EndAccept called");

						isConnected = true;
						serverCon.Send(new byte[256]);
					}, null);

					try
					{
						client.Connect(server.LocalEndPoint);
						Console.WriteLine("socket.Connected: {0} to {1}", client.Connected, client.RemoteEndPoint);
					}
					catch (Exception e)
					{
						throw new Exception(string.Format("Connect failed: {0}", e.Message), e);
					}

					client.Send(new byte[256]);

					try
					{
						int length = client.Receive(new byte[256]);
						length.Should().Be(256);
					}
					catch (Exception e)
					{
						throw new Exception(string.Format("Receive #{0} failed (Connected: {1}): {2}",
														  i,
														  isConnected,
														  e.Message),
											e);
					}
				}
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this CI server")]
		[Description("Verifies that Connect() can establish a connection with an endpoint by specifying its name")]
		public void TestConnect2()
		{
			using (var discoverer = new NetworkServiceDiscoverer())
			using (var client = CreateClient(name: "Rep1", networkServiceDiscoverer: discoverer))
			using (var server = CreateServer(name: "Rep2", networkServiceDiscoverer: discoverer))
			{
				Bind(server);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// ReSharper disable AccessToDisposedClosure
				new Action(() => Connect(client, server.Name, TimeSpan.FromSeconds(10)))
					// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
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

		protected override EndPoint EndPoint1
		{
			get { return new IPEndPoint(IPAddress.Loopback, 50012); }
		}

		protected override EndPoint EndPoint2
		{
			get { return new IPEndPoint(IPAddress.Loopback, 12345); }
		}

		protected override EndPoint EndPoint3
		{
			get { return new IPEndPoint(IPAddress.Loopback, 54321); }
		}

		protected override EndPoint EndPoint4
		{
			get { return new IPEndPoint(IPAddress.Loopback, 58752); }
		}

		protected override EndPoint EndPoint5
		{
			get { return new IPEndPoint(IPAddress.Loopback, 1234); }
		}

		protected override ConnectionId Connect(IRemotingEndPoint endPoint, EndPoint address)
		{
			return ((SocketRemotingEndPointClient) endPoint).Connect((IPEndPoint) address);
		}

		protected override void Connect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout)
		{
			((SocketRemotingEndPointClient) endPoint).Connect((IPEndPoint) address, timeout);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name)
		{
			((SocketRemotingEndPointClient) endPoint).Connect(name);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name, TimeSpan timeout)
		{
			((SocketRemotingEndPointClient)endPoint).Connect(name, timeout);
		}
	}
}
