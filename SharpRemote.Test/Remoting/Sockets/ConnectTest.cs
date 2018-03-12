using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Exceptions;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class ConnectTest
		: AbstractConnectTest
	{
		public override LogItem[] Loggers => new[]
		{
			new LogItem(typeof (SocketEndPoint)),
		};

		internal override IRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketEndPoint(EndPointType.Client,
			                          name,
			                          clientAuthenticator,
			                          serverAuthenticator,
			                          networkServiceDiscoverer,
			                          latencySettings: latencySettings,
			                          heartbeatSettings: heartbeatSettings);
		}

		internal override IRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, EndPointSettings endPointSettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
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

		[Test]
		[Description(
			"Verifies that Connect() cannot establish a connection with a non-existant endpoint and returns in the specified timeout"
			)]
		public void TestConnect3()
		{
			using (var rep = CreateClient())
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(100);
				new Action(
					() => new Action(() => Connect(rep, EndPoint1, timeout))
							  .ShouldThrow<NoSuchIPEndPointException>()
							  .WithMessage("Unable to establish a connection with the given endpoint after 100 ms: 127.0.0.1:50012"))
					.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(2));

				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteEndPoint.Should().BeNull(reason);
			}
		}

		[Test]
		[Description("Verifies that when Connect throws before the timeout is reached, the exception is handled gracefully (and not thrown on the finalizer thread)")]
		public void TestConnect18()
		{
			using (var client = CreateClient(name: "Rep1"))
			{
				var exceptions = new List<Exception>();
				TaskScheduler.UnobservedTaskException += (sender, args) =>
				{
					exceptions.Add(args.Exception);
					args.SetObserved();
				};

				new Action(() => Connect(client, EndPoint5, TimeSpan.FromMilliseconds(1)))
					.ShouldThrow<NoSuchIPEndPointException>();

				Thread.Sleep(2000);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				exceptions.Should().BeEmpty();
			}
		}

		[Test]
		[Description(
			"Verifies that Connect() throws when the other socket doesn't respond with the proper greeting message in time")]
		public void TestConnect8()
		{
			using (var rep = CreateClient())
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				socket.Bind(new IPEndPoint(IPAddress.Loopback, 54321));
				socket.Listen(1);
				socket.BeginAccept(ar => socket.EndAccept(ar), null);
				new Action(() => Connect(rep, EndPoint3))
					.ShouldThrow<AuthenticationException>();
			}
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((ISocketEndPoint)endPoint).Bind(IPAddress.Loopback);
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((ISocketEndPoint)endPoint).Bind((IPEndPoint) address);
		}

		protected override EndPoint EndPoint1 => new IPEndPoint(IPAddress.Loopback, 50012);

		protected override EndPoint EndPoint2 => new IPEndPoint(IPAddress.Loopback, 12345);

		protected override EndPoint EndPoint3 => new IPEndPoint(IPAddress.Loopback, 54321);

		protected override EndPoint EndPoint4 => new IPEndPoint(IPAddress.Loopback, 58752);

		protected override EndPoint EndPoint5 => new IPEndPoint(IPAddress.Loopback, 1234);

		protected override ConnectionId Connect(IRemotingEndPoint endPoint, EndPoint address)
		{
			return ((SocketEndPoint) endPoint).Connect((IPEndPoint) address);
		}

		protected override void Connect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout)
		{
			((SocketEndPoint) endPoint).Connect((IPEndPoint) address, timeout);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name)
		{
			((SocketEndPoint) endPoint).Connect(name);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name, TimeSpan timeout)
		{
			((SocketEndPoint)endPoint).Connect(name, timeout);
		}
	}
}
