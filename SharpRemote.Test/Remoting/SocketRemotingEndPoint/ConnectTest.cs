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

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	[TestFixture]
	public sealed class ConnectTest
		: AbstractTest
	{
		public override Type[] Loggers
		{
			get
			{
				return new[]
					{
						typeof (SocketRemotingEndPointClient),
						typeof (SocketRemotingEndPointServer)
					};
			}
		}

		[Test]
		[Description("Verifies that Connect() can establish a connection with an endpoint in the same process")]
		public void TestConnect1()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// ReSharper disable AccessToDisposedClosure
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this shitty CI server")]
		[Description("Verifies that Connect() can establish a connection with an endpoint by specifying its name")]
		public void TestConnect2()
		{
			using (var discoverer = new NetworkServiceDiscoverer())
			using (var client = CreateClient("Rep1", networkServiceDiscoverer: discoverer))
			using (var server = CreateServer("Rep2", networkServiceDiscoverer: discoverer))
			{
				server.Bind(IPAddress.Loopback);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// ReSharper disable AccessToDisposedClosure
				new Action(() => client.Connect(server.Name, TimeSpan.FromSeconds(10)))
					// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this shitty CI server")]
		[Description("Verifies that TryConnect() can establish a connection with an endpoint by specifying its name")]
		public void TestConnect23()
		{
			using (var discoverer = new NetworkServiceDiscoverer())
			using (var client = CreateClient("Rep1", networkServiceDiscoverer: discoverer))
			using (var server = CreateServer("Rep2", networkServiceDiscoverer: discoverer))
			{
				server.Bind(IPAddress.Loopback);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				client.TryConnect(server.Name, TimeSpan.FromSeconds(10))
				      .Should().BeTrue();

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
					() => new Action(() => rep.Connect(new IPEndPoint(IPAddress.Loopback, 50012), timeout))
							  .ShouldThrow<NoSuchIPEndPointException>()
							  .WithMessage("Unable to establish a connection with the given endpoint after 100 ms: 127.0.0.1:50012"))
					.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));

				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteEndPoint.Should().BeNull(reason);
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that Connect() cannot be called on an already connected endpoint")]
		public void TestConnect4()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server1 = CreateServer("Rep#2"))
			using (var server2 = CreateServer("Rep#3"))
			{
				server1.Bind(IPAddress.Loopback);
				server2.Bind(IPAddress.Loopback);

				TimeSpan timeout = TimeSpan.FromSeconds(5);
				client.Connect(server1.LocalEndPoint, timeout);
				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server1.LocalEndPoint);

				new Action(() => client.Connect(server2.LocalEndPoint, timeout))
					.ShouldThrow<InvalidOperationException>();
				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server1.LocalEndPoint);

				server2.IsConnected.Should().BeFalse();
				server2.RemoteEndPoint.Should().BeNull();
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a null address is given")]
		public void TestConnect5()
		{
			using (var rep = CreateClient())
			{
				new Action(() => rep.Connect((IPEndPoint)null, TimeSpan.FromSeconds(1)))
					.ShouldThrow<ArgumentNullException>()
					.WithMessage("Value cannot be null.\r\nParameter name: endpoint");
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a zero timeout is given")]
		public void TestConnect6()
		{
			using (var rep = CreateClient())
			{
				new Action(
					() => rep.Connect(new IPEndPoint(IPAddress.Loopback, 12345), TimeSpan.FromSeconds(0)))
					.ShouldThrow<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a negative timeout is given")]
		public void TestConnect7()
		{
			using (var rep = CreateClient())
			{
				new Action(
					() => rep.Connect(new IPEndPoint(IPAddress.Loopback, 12345), TimeSpan.FromSeconds(-1)))
					.ShouldThrow<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
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
				new Action(() => rep.Connect(new IPEndPoint(IPAddress.Loopback, 54321)))
					.ShouldThrow<AuthenticationException>();
			}
		}

		[Test]
		[LocalTest("Wont run on the shitty CI server")]
		[Description("Verifies that Connect() succeeds when client-side authentication is enabled and the challenge is met")]
		public void TestConnect9()
		{
			var authenticator = new TestAuthenticator();
			using (var client = CreateClient("Rep1", authenticator))
			using (var server = CreateServer("Rep2", authenticator))
			{
				server.Bind(new IPEndPoint(IPAddress.Loopback, 58752));
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when client-side authentication is enabled and the challenge is not met")]
		public void TestConnect10()
		{
			var wrongAuthenticator = new Test2Authenticator();
			var actualAuthenticator = new TestAuthenticator();
			using (var client = CreateClient("Rep1", wrongAuthenticator))
			using (var server = CreateServer("Rep2", actualAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when server-side authentication is enabled and the challenge is met")]
		public void TestConnect11()
		{
			var authenticator = new TestAuthenticator();
			using (var client = CreateClient("Rep1", null, authenticator))
			using (var server = CreateServer("Rep2", null, authenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when server-side authentication is enabled and the challenge is not met")]
		public void TestConnect12()
		{
			var wrongAuthenticator = new Test2Authenticator();
			var actualAuthenticator = new TestAuthenticator();
			using (var client = CreateClient("Rep1", null, actualAuthenticator))
			using (var server = CreateServer("Rep2", null, wrongAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when both server and client side authentication is enabled and both challenges are met")]
		public void TestConnect13()
		{
			var clientAuthenticator = new Test2Authenticator();
			var serverAuthenticator = new TestAuthenticator();
			using (var client = CreateClient("Rep1", clientAuthenticator, serverAuthenticator))
			using (var server = CreateServer("Rep2", clientAuthenticator, serverAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when both server and client side authentication is enabled and the client-side challenge is not met")]
		public void TestConnect14()
		{
			var serverAuthenticator = new TestAuthenticator();
			using (var client = CreateClient("Rep1", new TestAuthenticator(), serverAuthenticator))
			using (var server = CreateServer("Rep2", new Test2Authenticator(), serverAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when both server and client side authentication is enabled and the server-side challenge is not met")]
		public void TestConnect15()
		{
			var clientAuthenticator = new Test2Authenticator();
			using (var client = CreateClient("Rep1", clientAuthenticator, new TestAuthenticator()))
			using (var server = CreateServer("Rep2", clientAuthenticator, new Test2Authenticator()))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when client side authentication is enabled but the client doesn't provide any")]
		public void TestConnect16()
		{
			using (var client = CreateClient("Rep1"))
			using (var server = CreateServer("Rep2", new TestAuthenticator()))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldThrow<AuthenticationRequiredException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when server side authentication is enabled but the server doesn't provide any")]
		public void TestConnect17()
		{
			using (var client = CreateClient("Rep1", null, new TestAuthenticator()))
			using (var server = CreateServer("Rep2"))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint))
					.ShouldThrow<HandshakeException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that when Connect throws before the timeout is reached, the exception is handled gracefully (and not thrown on the finalizer thread)")]
		public void TestConnect18()
		{
			using (var client = CreateClient("Rep1"))
			{
				var exceptions = new List<Exception>();
				TaskScheduler.UnobservedTaskException += (sender, args) =>
					{
						exceptions.Add(args.Exception);
						args.SetObserved();
					};

				new Action(() => client.Connect(new IPEndPoint(IPAddress.Loopback, 1234), TimeSpan.FromMilliseconds(1)))
					.ShouldThrow<NoSuchIPEndPointException>();

				Thread.Sleep(2000);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				exceptions.Should().BeEmpty();
			}
		}

		[Test]
		[Description("Verifies that when TryConnect fails before the timeout is reached, the exception is handled gracefully (and not thrown on the finalizer thread)")]
		public void TestConnect19()
		{
			using (var client = CreateClient("Rep1"))
			{
				var exceptions = new List<Exception>();
				TaskScheduler.UnobservedTaskException += (sender, args) =>
				{
					exceptions.Add(args.Exception);
					args.SetObserved();
				};

				client.TryConnect(new IPEndPoint(IPAddress.Loopback, 1234), TimeSpan.FromMilliseconds(1)).Should().BeFalse();

				Thread.Sleep(2000);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				exceptions.Should().BeEmpty();
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this shitty CI server")]
		[Description("Verifies that establishing a connection to an already connected server is not allowed")]
		public void TestConnect20()
		{
			using (var client1 = CreateClient())
			using (var client2 = CreateClient())
			using (var server = CreateServer())
			{
				server.Bind(IPAddress.Loopback);
				client1.Connect(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);

				new Action(() => client2.Connect(server.LocalEndPoint))
					.ShouldThrow<SharpRemoteException>();
				client2.IsConnected.Should().BeFalse();

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("I swear to god, you cannot run any fucking test on this shitty CI server")]
		[Description("Verifies that establishing a connection to an already connected server is not allowed")]
		public void TestConnect21()
		{
			using (var client1 = CreateClient())
			using (var client2 = CreateClient())
			using (var server = CreateServer())
			{
				server.Bind(IPAddress.Loopback);
				client1.Connect(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);

				client2.TryConnect(server.LocalEndPoint).Should().BeFalse();
				client2.IsConnected.Should().BeFalse();

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that when one client ends a connection, another one can establish one to that server again")]
		public void TestConnect22()
		{
			using (var client1 = CreateClient())
			using (var client2 = CreateClient())
			using (var server = CreateServer())
			{
				server.Bind(IPAddress.Loopback);
				client1.Connect(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);

				server.Disconnect();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				client2.Connect(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client2.LocalEndPoint);
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this shitty CI server")]
		[Description("Verifies that the OnConnected event is fired for both the client and server when a connection is successfully established")]
		public void TestConnect24()
		{
			using (var client = CreateClient())
			using (var server = CreateServer())
			{
				var clients = new List<EndPoint>();
				var servers = new List<EndPoint>();
				client.OnConnected += (ep, unused) => clients.Add(ep);
				server.OnConnected += (ep, unused) => servers.Add(ep);

				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint);

				WaitFor(() => server.IsConnected, TimeSpan.FromSeconds(1));

				clients.Should().Equal(client.RemoteEndPoint);
				servers.Should().Equal(server.RemoteEndPoint);
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this shitty CI server")]
		[Description("Verifies that the OnConnected event is fired for both the client and server when a connection is successfully established")]
		public void TestConnect25()
		{
			using (var client = CreateClient())
			using (var server = CreateServer())
			{
				var clients = new List<EndPoint>();
				var servers = new List<EndPoint>();
				client.OnConnected += (ep, unused) => clients.Add(ep);
				server.OnConnected += (ep, unused) => servers.Add(ep);

				server.Bind(IPAddress.Loopback);
				client.TryConnect(server.LocalEndPoint).Should().BeTrue();

				WaitFor(() => server.IsConnected, TimeSpan.FromSeconds(1));

				clients.Should().Equal(client.RemoteEndPoint);
				servers.Should().Equal(server.RemoteEndPoint);
			}
		}

		[Test]
		[LocalTest("Wont run on the shitty CI server")]
		[Description("Verifies that after a connection is established, latency measurements are performed")]
		public void TestConnect26()
		{
			var settings = new LatencySettings
				{
					Interval = TimeSpan.FromTicks(10),
					NumSamples = 20
				};

			using (var client = CreateClient(latencySettings: settings))
			using (var server = CreateServer(latencySettings: settings))
			{
				client.RoundtripTime.Should().Be(TimeSpan.Zero);
				server.RoundtripTime.Should().Be(TimeSpan.Zero);

				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint);
				Thread.Sleep(TimeSpan.FromMilliseconds(100));

				var clientRoundtrip = client.RoundtripTime;
				var serverRoundtrip = server.RoundtripTime;

				Console.WriteLine("Client: {0}μs", clientRoundtrip.Ticks / 10);
				Console.WriteLine("Server: {0}μs", serverRoundtrip.Ticks / 10);

				clientRoundtrip.Should().BeGreaterThan(TimeSpan.Zero);
				serverRoundtrip.Should().BeGreaterThan(TimeSpan.Zero);
			}
		}

		[Test]
		[LocalTest("Doesn't work on AppVeyor for some reason")]
		[Description("Verifies that connections can be created and closed as fast as possible")]
		public void TestConnect27()
		{
			var heartbeatSettings = new HeartbeatSettings
				{
					UseHeartbeatFailureDetection = false
				};
			var latencySettings = new LatencySettings
				{
					PerformLatencyMeasurements = false
				};


			for (int i = 0; i < 100; ++i)
			{
				using (
					SocketRemotingEndPointClient client = CreateClient("perf_client", latencySettings: latencySettings,
					                                                   heartbeatSettings: heartbeatSettings))
				{
					using (
						SocketRemotingEndPointServer server = CreateServer("perf_server", latencySettings: latencySettings,
						                                                   heartbeatSettings: heartbeatSettings))
					{
						server.Bind(IPAddress.Loopback);
						client.Connect(server.LocalEndPoint);
						client.Disconnect();
					}
				}
			}
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
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that Connect() returns the same ConnectionId as the CurrentConnectionId after Connect() has been called")]
		public void TestConnect29()
		{
			using (var client = CreateClient())
			using (var server = CreateServer())
			{
				server.Bind(IPAddress.Loopback);

				var id = client.Connect(server.LocalEndPoint);
				id.Should().Be(new ConnectionId(1));
				client.CurrentConnectionId.Should().Be(new ConnectionId(1));
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that after accepting an incoming connection, the CurrentConnectionId is no longer set to none")]
		public void TestConnect30()
		{
			using (var client = CreateClient())
			using (var server = CreateServer())
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint);
				server.CurrentConnectionId.Should().Be(new ConnectionId(1));
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that new connection id is generated when the Connect() is called a second time")]
		public void TestConnect31()
		{
			using (var client = CreateClient())
			using (var server = CreateServer())
			{
				server.Bind(IPAddress.Loopback);

				var first = client.Connect(server.LocalEndPoint);
				client.Disconnect();
				var second = client.Connect(server.LocalEndPoint);

				first.Should().NotBe(second);
				second.Should().Be(new ConnectionId(2));
			}
		}
	}
}
