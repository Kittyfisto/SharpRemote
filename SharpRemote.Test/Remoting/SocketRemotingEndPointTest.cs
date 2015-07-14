using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Exceptions;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Core;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public class SocketRemotingEndPointTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<SocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<AbstractSocketRemotingEndPoint>(Level.Info);
		}

		protected SocketRemotingEndPoint CreateEndPoint(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null)
		{
			return new SocketRemotingEndPoint(name, clientAuthenticator, serverAuthenticator);
		}

		private static bool WaitFor(Func<bool> fn, TimeSpan timeout)
		{
			DateTime start = DateTime.Now;
			DateTime now = start;
			while ((now - start) < timeout)
			{
				if (fn())
					return true;

				Thread.Sleep(TimeSpan.FromMilliseconds(10));

				now = DateTime.Now;
			}

			return false;
		}

		[Test]
		[Description(
			"Verifies that when the connection between two endpoints is interrupted from the calling end, any ongoing synchronous method call is stopped and an exception is thrown on the calling thread"
			)]
		public void TestCallMethod1()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep#1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep#2"))
			{
				rep2.Bind(IPAddress.Loopback);
				rep1.Connect(rep2.LocalEndPoint, TimeSpan.FromSeconds(1));

				var subject = new Mock<IGetDoubleProperty>();
				subject.Setup(x => x.Value).Returns(() =>
					{
						// We interrupt the connection from the calling endpoint itself
						rep1.Disconnect();
						return 42;
					});

				const int id = 1;
				rep2.CreateServant(id, subject.Object);
				var proxy = rep1.CreateProxy<IGetDoubleProperty>(id);

				new Action(() => { double unused = proxy.Value; })
					.ShouldThrow<ConnectionLostException>()
					.WithMessage("The connection to the remote endpoint has been lost");
			}
		}

		[Test]
		[Description(
			"Verifies that when the connection between two endpoints is interrupted from the called end, any ongoing synchronous method call is stopped and an exception is thrown on the calling thread"
			)]
		public void TestCallMethod2()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep#1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep#2"))
			{
				rep2.Bind(IPAddress.Loopback);
				rep1.Connect(rep2.LocalEndPoint, TimeSpan.FromSeconds(1));

				var subject = new Mock<IGetDoubleProperty>();
				subject.Setup(x => x.Value).Returns(() =>
					{
						// We interrupt the connection from the called endpoint
						rep2.Disconnect();
						return 42;
					});

				const int id = 1;
				rep2.CreateServant(id, subject.Object);
				var proxy = rep1.CreateProxy<IGetDoubleProperty>(id);

				new Action(() => { double unused = proxy.Value; })
					.ShouldThrow<ConnectionLostException>()
					.WithMessage("The connection to the remote endpoint has been lost");
			}
		}

		[Test]
		[Description("Verifies that Connect() can establish a connection with an endpoint in the same process")]
		public void TestConnect1()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep#1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep#2"))
			{
				rep2.Bind(IPAddress.Loopback);

				rep1.IsConnected.Should().BeFalse();
				rep1.RemoteEndPoint.Should().BeNull();

				rep2.IsConnected.Should().BeFalse();
				rep2.RemoteEndPoint.Should().BeNull();

// ReSharper disable AccessToDisposedClosure
				new Action(() => rep1.Connect(rep2.LocalEndPoint, TimeSpan.FromSeconds(1)))
// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteEndPoint.Should().Be(rep2.LocalEndPoint);

				rep2.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Ignore("TODO: Find out why this test wont work - it should :P")]
		[LocalTest]
		[Description("Verifies that Connect() can establish a connection with an endpoint by specifying its name")]
		public void TestConnect2()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep2"))
			{
				rep2.Bind(IPAddress.Loopback);

				rep1.IsConnected.Should().BeFalse();
				rep1.RemoteEndPoint.Should().BeNull();

				rep2.IsConnected.Should().BeFalse();
				rep2.RemoteEndPoint.Should().BeNull();

				// ReSharper disable AccessToDisposedClosure
				new Action(() => rep1.Connect(rep2.Name, TimeSpan.FromSeconds(1)))
					// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteEndPoint.Should().Be(rep2.LocalEndPoint);

				rep2.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description(
			"Verifies that Connect() cannot establish a connection with a non-existant endpoint and returns in the specified timeout"
			)]
		public void TestConnect3()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(100);
				new Action(
					() => new Action(() => rep.Connect(new IPEndPoint(IPAddress.Loopback, 50012), timeout))
						      .ShouldThrow<NoSuchIPEndPointException>()
						      .WithMessage("Unable to establish a connection with the given endpoint: 127.0.0.1:50012"))
					.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));

				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteEndPoint.Should().BeNull(reason);
			}
		}

		[Test]
		[Description("Verifies that Connect() cannot establish a connection with itself")]
		public void TestConnect4()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				rep.Bind(IPAddress.Loopback);

				TimeSpan timeout = TimeSpan.FromMilliseconds(100);
				const string message = "An endpoint cannot be connected to itself\r\nParameter name: endpoint";
				new Action(() => rep.Connect(rep.LocalEndPoint, timeout))
					.ShouldThrow<ArgumentException>()
					.WithMessage(message);

				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteEndPoint.Should().BeNull(reason);

				new Action(() => rep.Connect(rep.LocalEndPoint, timeout))
					.ShouldThrow<ArgumentException>()
					.WithMessage(message);

				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteEndPoint.Should().BeNull(reason);
			}
		}

		[Test]
		[Description("Verifies that Connect() cannot be called on an already connected endpoint")]
		public void TestConnect5()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep#1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep#2"))
			using (SocketRemotingEndPoint rep3 = CreateEndPoint("Rep#3"))
			{
				rep2.Bind(IPAddress.Loopback);
				rep3.Bind(IPAddress.Loopback);

				TimeSpan timeout = TimeSpan.FromSeconds(1);
				rep1.Connect(rep2.LocalEndPoint, timeout);
				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteEndPoint.Should().Be(rep2.LocalEndPoint);

				new Action(() => rep1.Connect(rep3.LocalEndPoint, timeout))
					.ShouldThrow<InvalidOperationException>();
				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteEndPoint.Should().Be(rep2.LocalEndPoint);

				rep3.IsConnected.Should().BeFalse();
				rep3.RemoteEndPoint.Should().BeNull();
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a null address is given")]
		public void TestConnect6()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				new Action(() => rep.Connect((IPEndPoint) null, TimeSpan.FromSeconds(1)))
					.ShouldThrow<ArgumentNullException>()
					.WithMessage("Value cannot be null.\r\nParameter name: endpoint");
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a zero timeout is given")]
		public void TestConnect7()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				new Action(
					() => rep.Connect(new IPEndPoint(IPAddress.Loopback, 12345), TimeSpan.FromSeconds(0)))
					.ShouldThrow<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a negative timeout is given")]
		public void TestConnect8()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
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
		public void TestConnect9()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.Bind(new IPEndPoint(IPAddress.Loopback, 54321));
				socket.Listen(1);
				socket.BeginAccept(ar => socket.EndAccept(ar), null);
				new Action(() => rep.Connect(new IPEndPoint(IPAddress.Loopback, 54321)))
					.ShouldThrow<AuthenticationException>();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when client-side authentication is enabled and the challenge is met")]
		public void TestConnect10()
		{
			var authenticator = new TestAuthenticator();
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1", authenticator))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", authenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint)).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when client-side authentication is enabled and the challenge is not met")]
		public void TestConnect11()
		{
			var wrongAuthenticator = new Test2Authenticator();
			var actualAuthenticator = new TestAuthenticator();
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1", wrongAuthenticator))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", actualAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when server-side authentication is enabled and the challenge is met")]
		public void TestConnect12()
		{
			var authenticator = new TestAuthenticator();
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1", null, authenticator))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", null, authenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint)).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when server-side authentication is enabled and the challenge is not met")]
		public void TestConnect13()
		{
			var wrongAuthenticator = new Test2Authenticator();
			var actualAuthenticator = new TestAuthenticator();
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1", null, actualAuthenticator))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", null, wrongAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when both server and client side authentication is enabled and both challenges are met")]
		public void TestConnect14()
		{
			var clientAuthenticator = new Test2Authenticator();
			var serverAuthenticator = new TestAuthenticator();
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1", clientAuthenticator, serverAuthenticator))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", clientAuthenticator, serverAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint)).ShouldNotThrow();
				server.IsConnected.Should().BeTrue();
				client.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Connect() fails when both server and client side authentication is enabled and the client-side challenge is not met")]
		public void TestConnect15()
		{
			var serverAuthenticator = new TestAuthenticator();
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1", new TestAuthenticator(), serverAuthenticator))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", new Test2Authenticator(), serverAuthenticator))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Connect() succeeds when both server and client side authentication is enabled and the server-side challenge is not met")]
		public void TestConnect16()
		{
			var clientAuthenticator = new Test2Authenticator();
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1", clientAuthenticator, new TestAuthenticator()))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", clientAuthenticator, new Test2Authenticator()))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint))
					.ShouldThrow<AuthenticationException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Ignore]
		[Description("Verifies that Connect() succeeds when both client side authentication is enabled but the client doesn't provide any")]
		public void TestConnect17()
		{
			using (SocketRemotingEndPoint client = CreateEndPoint("Rep1"))
			using (SocketRemotingEndPoint server = CreateEndPoint("Rep2", new TestAuthenticator()))
			{
				server.Bind(IPAddress.Loopback);
				new Action(() => client.Connect(server.LocalEndPoint))
					.ShouldThrow<AuthenticationRequiredException>();
				server.IsConnected.Should().BeFalse();
				client.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that a proxy on an unconnected endpoint can be created")]
		public void TestCreateProxy1()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				IDisposable proxy = null;

				const string reason =
					"because a proxy can always be created - its usage may however not work depending on the endpoint's connection status";
				new Action(() => proxy = rep.CreateProxy<IDisposable>(0))
					.ShouldNotThrow(reason);
				proxy.Should().NotBeNull(reason);

				new Action(() => proxy.Dispose())
					.ShouldThrow<NotConnectedException>(
						"because the endpoint is not connected to any other endpoint and thus there cannot be a subject on which the method can ever be executed");
			}
		}

		[Test]
		[Description("Verifies that a servant on an unconnected endpoint can be created")]
		public void TestCreateServant1()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				var subject = new Mock<IEventInt32>();
				IServant servant = null;

				const string reason =
					"because a servant can always be created - its usage may however not work depending on the endpoint's connection status";
				new Action(() => servant = rep.CreateServant(0, subject.Object))
					.ShouldNotThrow(reason);
				servant.Should().NotBeNull(reason);

				new Action(() => subject.Raise(x => x.Foobar += null, 42))
					.ShouldThrow<NotConnectedException>(
						"because the endpoint is not connected to any other endpoint and thus there cannot be a proxy on which the event can ever be executed");
			}
		}

		[Test]
		[Description("Verifies that creating a peer-endpoint without specifying a port works and assigns a free port")]
		public void TestCtor1()
		{
			using (SocketRemotingEndPoint rep = CreateEndPoint())
			{
				rep.Bind(IPAddress.Loopback);

				rep.LocalEndPoint.Should().NotBeNull();
				rep.LocalEndPoint.Address.Should().Be(IPAddress.Loopback);
				rep.LocalEndPoint.Port.Should()
				   .BeInRange(49152, 65535,
				              "because an automatically chosen port should be in the range of private/dynamic port numbers");
				rep.RemoteEndPoint.Should().BeNull();
				rep.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect1()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep#1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep#2"))
			{
				rep2.Bind(IPAddress.Loopback);

				rep1.Connect(rep2.LocalEndPoint, TimeSpan.FromSeconds(1));

				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteEndPoint.Should().Be(rep2.LocalEndPoint);

				// Disconnecting from the endpoint that established the connection in the first place
				rep1.Disconnect();

				rep1.IsConnected.Should().BeFalse();
				rep1.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep2 is disconnected as well...
			}
		}

		[Test]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect2()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep#1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep#2"))
			{
				rep2.Bind(IPAddress.Loopback);
				rep1.Connect(rep2.LocalEndPoint, TimeSpan.FromSeconds(1));

				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteEndPoint.Should().Be(rep2.LocalEndPoint);

				// Disconnecting from the other endpoint
				rep2.Disconnect();

				rep2.IsConnected.Should().BeFalse();
				rep2.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep1 is disconnected as well...
			}
		}

		[Test]
		[Description("Verifies that disconnecting and connecting to the same endpoint again is possible")]
		public void TestDisconnect3()
		{
			using (SocketRemotingEndPoint rep1 = CreateEndPoint("Rep#1"))
			using (SocketRemotingEndPoint rep2 = CreateEndPoint("Rep#2"))
			{
				rep2.Bind(IPAddress.Loopback);
				rep1.Connect(rep2.LocalEndPoint, TimeSpan.FromSeconds(1));
				rep1.IsConnected.Should().BeTrue();
				rep2.IsConnected.Should().BeTrue();

				rep1.Disconnect();
				rep1.IsConnected.Should().BeFalse();
				WaitFor(() => !rep2.IsConnected, TimeSpan.FromSeconds(2))
					.Should().BeTrue();

				new Action(() => rep1.Connect(rep2.LocalEndPoint, TimeSpan.FromSeconds(1)))
					.ShouldNotThrow();
				rep1.IsConnected.Should().BeTrue();
				rep2.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that disposing the endpoint actually closes the listening socket")]
		public void TestDispose()
		{
			IPEndPoint endpoint;
			using (SocketRemotingEndPoint ep = CreateEndPoint("Foo"))
			{
				ep.Bind(IPAddress.Loopback);
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