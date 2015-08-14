using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Exceptions;

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	[TestFixture]
	public sealed class ConnectTest
		: AbstractTest
	{
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
		[Description("Verifies that Connect() can establish a connection with an endpoint by specifying its name")]
		public void TestConnect2()
		{
			using (var client = CreateClient("Rep1"))
			using (var server = CreateServer("Rep2"))
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
	}
}
