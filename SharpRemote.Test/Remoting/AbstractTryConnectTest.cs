using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting
{
	public abstract class AbstractTryConnectTest
		: AbstractEndPointTest
	{
		protected abstract bool TryConnect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout);
		protected abstract bool TryConnect(IRemotingEndPoint endPoint, EndPoint address);
		protected abstract bool TryConnect(IRemotingEndPoint endPoint, string name, TimeSpan timeout);
		protected abstract bool TryConnect(IRemotingEndPoint endPoint, string name);
		protected abstract void Connect(IRemotingEndPoint endPoint, EndPoint address);

		protected abstract EndPoint EndPoint1 { get; }
		protected abstract EndPoint EndPoint2 { get; }

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
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

				Bind(server);
				TryConnect(client, server.LocalEndPoint).Should().BeTrue();

				WaitFor(() => server.IsConnected, TimeSpan.FromSeconds(1));

				clients.Should().Equal(client.RemoteEndPoint);
				servers.Should().Equal(server.RemoteEndPoint);
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that TryConnect() can establish a connection with an endpoint in the same process")]
		public void TestTryConnect1()
		{
			using (var client = CreateClient(name: "Rep1"))
			using (var server = CreateServer(name: "Rep2"))
			{
				Bind(server, EndPoint1);
				bool success = false;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				success.Should().BeTrue();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that when TryConnect fails before the timeout is reached, the exception is handled gracefully (and not thrown on the finalizer thread)")]
		public void TestTryConnect19()
		{
			using (var client = CreateClient(name: "Rep1"))
			{
				var exceptions = new List<Exception>();
				TaskScheduler.UnobservedTaskException += (sender, args) =>
				{
					exceptions.Add(args.Exception);
					args.SetObserved();
				};

				TryConnect(client, EndPoint2, TimeSpan.FromMilliseconds(1)).Should().BeFalse();

				Thread.Sleep(2000);

				GC.Collect();
				GC.WaitForPendingFinalizers();

				exceptions.Should().BeEmpty();
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that establishing a connection to an already connected server is not allowed")]
		public void TestConnect21()
		{
			using (var client1 = CreateClient())
			using (var client2 = CreateClient())
			using (var server = CreateServer())
			{
				Bind(server);
				Connect(client1, server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);

				TryConnect(client2, server.LocalEndPoint).Should().BeFalse();
				client2.IsConnected.Should().BeFalse();

				server.IsConnected.Should().BeTrue();
				server.RemoteEndPoint.Should().Be(client1.LocalEndPoint);
			}
		}

		[Test]
		[Description(
			"Verifies that TryConnect() cannot establish a connection with a non-existant endpoint and returns in the specified timeout"
			)]
		public void TestTryConnect3()
		{
			using (var rep = CreateClient())
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(100);
				bool success = true;
				new Action(
					() => success = TryConnect(rep, EndPoint2, timeout))
					.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(2));

				success.Should().BeFalse();
				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteEndPoint.Should().BeNull(reason);
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that TryConnect() cannot be called on an already connected endpoint")]
		public void TestTryConnect4()
		{
			using (var client = CreateClient(name: "Rep#1"))
			using (var server1 = CreateServer(name: "Rep#2"))
			using (var server2 = CreateServer(name: "Rep#3"))
			{
				Bind(server1);
				Bind(server2);

				TimeSpan timeout = TimeSpan.FromSeconds(5);
				TryConnect(client, server1.LocalEndPoint, timeout).Should().BeTrue();
				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server1.LocalEndPoint);

				new Action(() => TryConnect(client, server2.LocalEndPoint, timeout))
					.ShouldThrow<InvalidOperationException>();

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server1.LocalEndPoint);

				server2.IsConnected.Should().BeFalse();
				server2.RemoteEndPoint.Should().BeNull();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() throws when a null address is given")]
		public void TestTryConnect5()
		{
			using (var rep = CreateClient())
			{
				new Action(() => TryConnect(rep, (EndPoint)null, TimeSpan.FromSeconds(1)))
					.ShouldThrow<ArgumentNullException>()
					.WithMessage("Value cannot be null.\r\nParameter name: endpoint");
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that TryConnect() succeeds when client-side authentication is enabled and the challenge is met")]
		public void TestTryConnect9()
		{
			var authenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: authenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: authenticator))
			{
				Bind(server, EndPoint1);
				new Action(() => TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() fails when client-side authentication is enabled and the challenge is not met")]
		public void TestTryConnect10()
		{
			var wrongAuthenticator = new Test2Authenticator();
			var actualAuthenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: wrongAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: actualAuthenticator))
			{
				Bind(server);
				bool success = true;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldNotThrow();
				success.Should().BeFalse();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that TryConnect() succeeds when server-side authentication is enabled and the challenge is met")]
		public void TestTryConnect11()
		{
			var authenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: null, serverAuthenticator: authenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: null, serverAuthenticator: authenticator))
			{
				Bind(server);
				bool success = false;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				success.Should().BeTrue();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() fails when server-side authentication is enabled and the challenge is not met")]
		public void TestTryConnect12()
		{
			var wrongAuthenticator = new Test2Authenticator();
			var actualAuthenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: null, serverAuthenticator: actualAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: null, serverAuthenticator: wrongAuthenticator))
			{
				Bind(server);
				bool success = false;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldNotThrow();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that TryConnect() succeeds when both server and client side authentication is enabled and both challenges are met")]
		public void TestTryConnect13()
		{
			var clientAuthenticator = new Test2Authenticator();
			var serverAuthenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: clientAuthenticator, serverAuthenticator: serverAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: clientAuthenticator, serverAuthenticator: serverAuthenticator))
			{
				Bind(server);
				bool success = false;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10))).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() fails when both server and client side authentication is enabled and the client-side challenge is not met")]
		public void TestTryConnect14()
		{
			var serverAuthenticator = new TestAuthenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: new TestAuthenticator(), serverAuthenticator: serverAuthenticator))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: new Test2Authenticator(), serverAuthenticator: serverAuthenticator))
			{
				Bind(server);
				bool success = true;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldNotThrow();
				success.Should().BeFalse();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() fails when both server and client side authentication is enabled and the server-side challenge is not met")]
		public void TestTryConnect15()
		{
			var clientAuthenticator = new Test2Authenticator();
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: clientAuthenticator, serverAuthenticator: new TestAuthenticator()))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: clientAuthenticator, serverAuthenticator: new Test2Authenticator()))
			{
				Bind(server);
				bool success = false;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldNotThrow();
				success.Should().BeFalse();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() fails when client side authentication is enabled but the client doesn't provide any")]
		public void TestTryConnect16()
		{
			using (var client = CreateClient(name: "Rep1"))
			using (var server = CreateServer(name: "Rep2", clientAuthenticator: new TestAuthenticator()))
			{
				Bind(server);
				bool success = true;
				new Action(() => success = TryConnect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10)))
					.ShouldNotThrow();
				success.Should().BeFalse();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that TryConnect() fails when server side authentication is enabled but the server doesn't provide any")]
		public void TestConnect17()
		{
			using (var client = CreateClient(name: "Rep1", clientAuthenticator: null, serverAuthenticator: new TestAuthenticator()))
			using (var server = CreateServer(name: "Rep2"))
			{
				Bind(server);
				bool success = true;
				new Action(() => success = TryConnect(client, server.LocalEndPoint))
					.ShouldNotThrow();
				success.Should().BeFalse();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}
	}
}