using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Exceptions;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public abstract class AbstractConnectTest
		: AbstractEndPointTest
	{
		protected abstract EndPoint EndPoint1 { get; }
		protected abstract EndPoint EndPoint2 { get; }
		protected abstract EndPoint EndPoint3 { get; }
		protected abstract EndPoint EndPoint4 { get; }
		protected abstract EndPoint EndPoint5 { get; }
		protected abstract ConnectionId Connect(IRemotingEndPoint endPoint, EndPoint address);
		protected abstract void Connect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout);
		protected abstract void Connect(IRemotingEndPoint endPoint, string name);
		protected abstract void Connect(IRemotingEndPoint endPoint, string name, TimeSpan timeout);

		[Test]
		[Description("Verifies that Connect() can establish a connection with an endpoint in the same process")]
		public void TestConnect1()
		{
			using (var client = CreateClient(name: "Rep#1"))
			using (var server = CreateServer(name: "Rep#2"))
			{
				Bind(server);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// ReSharper disable AccessToDisposedClosure
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that Connect() cannot be called on an already connected endpoint")]
		public void TestConnect4()
		{
			using (var client = CreateClient(name: "Rep#1"))
			using (var server1 = CreateServer(name: "Rep#2"))
			using (var server2 = CreateServer(name: "Rep#3"))
			{
				Bind(server1);
				Bind(server2);

				TimeSpan timeout = TimeSpan.FromSeconds(5);
				Connect(client, server1.LocalEndPoint, timeout);
				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server1.LocalEndPoint);

				new Action(() => Connect(client, server2.LocalEndPoint, timeout))
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
				new Action(() => Connect(rep, (EndPoint)null, TimeSpan.FromSeconds(1)))
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
					() => Connect(rep, EndPoint2, TimeSpan.FromSeconds(0)))
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
					() => Connect(rep, EndPoint2, TimeSpan.FromSeconds(-1)))
					.ShouldThrow<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}

		[Test]
		[LocalTest("Wont run on the shitty CI server")]
		[Description("Verifies that Connect() succeeds when client-side authentication is enabled and the challenge is met")]
		public void TestConnect9()
		{
			var authenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: authenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: authenticator))
			{
				Bind(server, EndPoint4);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
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
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: wrongAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: actualAuthenticator))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
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
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: null, serverAuthenticator: authenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: null, serverAuthenticator: authenticator))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
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
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: null, serverAuthenticator: actualAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: null, serverAuthenticator: wrongAuthenticator))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
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
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: clientAuthenticator, serverAuthenticator: serverAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: clientAuthenticator, serverAuthenticator: serverAuthenticator))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when both server and client side authentication is enabled and the client-side challenge is not met")]
		public void TestConnect14()
		{
			var serverAuthenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: new TestAuthenticator(), serverAuthenticator: serverAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: new Test2Authenticator(), serverAuthenticator: serverAuthenticator))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
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
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: clientAuthenticator, serverAuthenticator: new TestAuthenticator()))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: clientAuthenticator, serverAuthenticator: new Test2Authenticator()))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when client side authentication is enabled but the client doesn't provide any")]
		public void TestConnect16()
		{
			using (var client = CreateClient(name: "Rep1"))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: new TestAuthenticator()))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldThrow<AuthenticationRequiredException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when server side authentication is enabled but the server doesn't provide any")]
		public void TestConnect17()
		{
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: null, serverAuthenticator: new TestAuthenticator()))
			using (var server = CreateServer(name: "Rep2"))
			{
				Bind(server);
				new Action(() => Connect(client, server.LocalEndPoint))
					.ShouldThrow<HandshakeException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
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
				Bind(server);
				Connect(client1, server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);

				new Action(() => Connect(client2, server.LocalEndPoint))
					.ShouldThrow<SharpRemoteException>();
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
				Bind(server);
				Connect(client1, server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);

				server.Disconnect();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				Connect(client2, server.LocalEndPoint);

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

				Bind(server);
				Connect(client, server.LocalEndPoint);

				WaitFor(() => servers.Count == 1, TimeSpan.FromSeconds(1)).Should().BeTrue();
				server.IsConnected.Should().BeTrue();

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

				Bind(server);
				Connect(client, server.LocalEndPoint);
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
					var client = CreateClient(name: "perf_client", latencySettings: latencySettings,
																	   heartbeatSettings: heartbeatSettings))
				{
					using (
						var server = CreateServer(name: "perf_server", latencySettings: latencySettings,
																		   heartbeatSettings: heartbeatSettings))
					{
						Bind(server);
						Connect(client, server.LocalEndPoint);
						client.Disconnect();
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
				Bind(server);

				var id = Connect(client, server.LocalEndPoint);
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
				Bind(server);
				Connect(client, server.LocalEndPoint);
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
				Bind(server);

				var first = Connect(client, server.LocalEndPoint);
				client.Disconnect();
				var second = Connect(client, server.LocalEndPoint);

				first.Should().NotBe(second);
				second.Should().Be(new ConnectionId(2));
			}
		}
	}
}