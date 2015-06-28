using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Core;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public class SocketEndPointTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<SocketEndPoint>(Level.Info);
		}

		protected SocketEndPoint CreateEndPoint(IPAddress address, string name = null)
		{
			return new SocketEndPoint(address, name);
		}

		[Test]
		[Description(
			"Verifies that when the connection between two endpoints is interrupted from the calling end, any ongoing synchronous method call is stopped and an exception is thrown on the calling thread"
			)]
		public void TestCallMethod1()
		{
			using (SocketEndPoint rep1 = CreateEndPoint(IPAddress.Loopback, "Rep#1"))
			using (SocketEndPoint rep2 = CreateEndPoint(IPAddress.Loopback, "Rep#2"))
			{
				rep1.Connect(rep2.LocalAddress, TimeSpan.FromSeconds(1));

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
			using (SocketEndPoint rep1 = CreateEndPoint(IPAddress.Loopback, "Rep#1"))
			using (SocketEndPoint rep2 = CreateEndPoint(IPAddress.Loopback, "Rep#2"))
			{
				rep1.Connect(rep2.LocalAddress, TimeSpan.FromSeconds(1));

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
			using (SocketEndPoint rep1 = CreateEndPoint(IPAddress.Loopback, "Rep#1"))
			using (SocketEndPoint rep2 = CreateEndPoint(IPAddress.Loopback, "Rep#2"))
			{
				rep1.IsConnected.Should().BeFalse();
				rep1.RemoteAddress.Should().BeNull();

				rep2.IsConnected.Should().BeFalse();
				rep2.RemoteAddress.Should().BeNull();

// ReSharper disable AccessToDisposedClosure
				new Action(() => rep1.Connect(rep2.LocalAddress, TimeSpan.FromSeconds(1)))
// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteAddress.Should().Be(rep2.LocalAddress);

				rep2.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description(
			"Verifies that Connect() cannot establish a connection with a non-existant endpoint and returns in the specified timeout"
			)]
		public void TestConnect2()
		{
			using (SocketEndPoint rep = CreateEndPoint(IPAddress.Loopback))
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(100);
				new Action(
					() => new Action(() => rep.Connect(new Uri(string.Format("tcp://{0}:{1}", IPAddress.Loopback, 50012)), timeout))
						      .ShouldThrow<NoSuchEndPointException>()
						      .WithMessage("Unable to establish a connection with the given endpoint: tcp://127.0.0.1:50012/"))
					.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));

				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteAddress.Should().BeNull(reason);
			}
		}

		[Test]
		[Description("Verifies that Connect() cannot establish a connection with itself")]
		public void TestConnect3()
		{
			using (SocketEndPoint rep = CreateEndPoint(IPAddress.Loopback))
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(100);
				const string message = "An endpoint cannot be connected to itself\r\nParameter name: Uri";
				new Action(() => rep.Connect(rep.LocalAddress, timeout))
					.ShouldThrow<ArgumentException>()
					.WithMessage(message);

				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteAddress.Should().BeNull(reason);

				new Action(() => rep.Connect(rep.LocalAddress, timeout))
					.ShouldThrow<ArgumentException>()
					.WithMessage(message);

				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteAddress.Should().BeNull(reason);
			}
		}

		[Test]
		[Description("Verifies that Connect() cannot be called on an already connected endpoint")]
		public void TestConnect4()
		{
			using (SocketEndPoint rep1 = CreateEndPoint(IPAddress.Loopback, "Rep#1"))
			using (SocketEndPoint rep2 = CreateEndPoint(IPAddress.Loopback, "Rep#2"))
			using (SocketEndPoint rep3 = CreateEndPoint(IPAddress.Loopback, "Rep#3"))
			{
				TimeSpan timeout = TimeSpan.FromSeconds(1);
				rep1.Connect(rep2.LocalAddress, timeout);
				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteAddress.Should().Be(rep2.LocalAddress);

				new Action(() => rep1.Connect(rep3.LocalAddress, timeout))
					.ShouldThrow<InvalidOperationException>();
				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteAddress.Should().Be(rep2.LocalAddress);

				rep3.IsConnected.Should().BeFalse();
				rep3.RemoteAddress.Should().BeNull();
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a null address is given")]
		public void TestConnect5()
		{
			using (SocketEndPoint rep = CreateEndPoint(IPAddress.Loopback))
			{
				new Action(() => rep.Connect(null, TimeSpan.FromSeconds(1)))
					.ShouldThrow<ArgumentNullException>()
					.WithMessage("Value cannot be null.\r\nParameter name: Uri");
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a zero timeout is given")]
		public void TestConnect6()
		{
			using (SocketEndPoint rep = CreateEndPoint(IPAddress.Loopback))
			{
				new Action(
					() => rep.Connect(new Uri(string.Format("tcp://{0}:{1}", IPAddress.Loopback, 12345)), TimeSpan.FromSeconds(0)))
					.ShouldThrow<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}

		[Test]
		[Description("Verifies that Connect() throws when a negative timeout is given")]
		public void TestConnect7()
		{
			using (SocketEndPoint rep = CreateEndPoint(IPAddress.Loopback))
			{
				new Action(
					() => rep.Connect(new Uri(string.Format("tcp://{0}:{1}", IPAddress.Loopback, 12345)), TimeSpan.FromSeconds(-1)))
					.ShouldThrow<ArgumentOutOfRangeException>()
					.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: timeout");
			}
		}

		[Test]
		[Description("Verifies that a proxy on an unconnected endpoint can be created")]
		public void TestCreateProxy1()
		{
			using (SocketEndPoint rep = CreateEndPoint(IPAddress.Loopback))
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
			using (SocketEndPoint rep = CreateEndPoint(IPAddress.Loopback))
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
			IPAddress address = IPAddress.Loopback;
			using (SocketEndPoint rep = CreateEndPoint(address))
			{
				rep.LocalAddress.Should().NotBeNull();
				rep.LocalAddress.Host.Should().Be(address.ToString());
				rep.LocalAddress.Port.Should()
				   .BeInRange(49152, 65535,
				              "because an automatically chosen port should be in the range of private/dynamic port numbers");
				rep.RemoteAddress.Should().BeNull();
				rep.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect1()
		{
			using (SocketEndPoint rep1 = CreateEndPoint(IPAddress.Loopback, "Rep#1"))
			using (SocketEndPoint rep2 = CreateEndPoint(IPAddress.Loopback, "Rep#2"))
			{
				rep1.Connect(rep2.LocalAddress, TimeSpan.FromSeconds(1));

				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteAddress.Should().Be(rep2.LocalAddress);

				// Disconnecting from the endpoint that established the connection in the first place
				rep1.Disconnect();

				rep1.IsConnected.Should().BeFalse();
				rep1.RemoteAddress.Should().BeNull();

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
			using (SocketEndPoint rep1 = CreateEndPoint(IPAddress.Loopback, "Rep#1"))
			using (SocketEndPoint rep2 = CreateEndPoint(IPAddress.Loopback, "Rep#2"))
			{
				rep1.Connect(rep2.LocalAddress, TimeSpan.FromSeconds(1));

				rep1.IsConnected.Should().BeTrue();
				rep1.RemoteAddress.Should().Be(rep2.LocalAddress);

				// Disconnecting from the other endpoint
				rep2.Disconnect();

				rep2.IsConnected.Should().BeFalse();
				rep2.RemoteAddress.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep1 is disconnected as well...
			}
		}

		[Test]
		[Description("Verifies that disconnecting and connecting to the same endpoint again is possible")]
		public void TestDisconnect3()
		{
			using (SocketEndPoint rep1 = CreateEndPoint(IPAddress.Loopback, "Rep#1"))
			using (SocketEndPoint rep2 = CreateEndPoint(IPAddress.Loopback, "Rep#2"))
			{
				rep1.Connect(rep2.LocalAddress, TimeSpan.FromSeconds(1));
				rep1.IsConnected.Should().BeTrue();
				rep2.IsConnected.Should().BeTrue();

				rep1.Disconnect();
				rep1.IsConnected.Should().BeFalse();
				WaitFor(() => !rep2.IsConnected, TimeSpan.FromSeconds(2))
					.Should().BeTrue();

				new Action(() => rep1.Connect(rep2.LocalAddress, TimeSpan.FromSeconds(1)))
					.ShouldNotThrow();
				rep1.IsConnected.Should().BeTrue();
				WaitFor(() => rep2.IsConnected, TimeSpan.FromSeconds(20))
					.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that disposing the endpoint actually closes the listening socket")]
		public void TestDispose()
		{
			IPEndPoint endpoint;
			using (SocketEndPoint ep = CreateEndPoint(IPAddress.Loopback, "Foo"))
			{
				endpoint = ep.LocalEndPoint;
			}

			// If the SocketEndPoint correctly disposed the listening socket, then
			// we should be able to create a new socket on the same address/port.
			using (var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
			{
				new Action(() => socket.Bind(endpoint))
					.ShouldNotThrow("Because the corresponding endpoint should no longer be in use");
			}
		}

		private static bool WaitFor(Func<bool> fn, TimeSpan timeout)
		{
			var start = DateTime.Now;
			var now = start;
			while ((now - start) < timeout)
			{
				if (fn())
					return true;

				Thread.Sleep(TimeSpan.FromMilliseconds(10));

				now = DateTime.Now;
			}

			return false;
		}
	}
}