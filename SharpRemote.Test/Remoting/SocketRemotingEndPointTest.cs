using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Exceptions;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
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
			TestLogger.SetLevel<AbstractSocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<AbstractIPSocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointClient>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointServer>(Level.Info);
		}

		protected SocketRemotingEndPointClient CreateClient(string name = null, IAuthenticator clientAuthenticator = null,
		                                                    IAuthenticator serverAuthenticator = null)
		{
			return new SocketRemotingEndPointClient(name, clientAuthenticator, serverAuthenticator);
		}

		protected SocketRemotingEndPointServer CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null)
		{
			return new SocketRemotingEndPointServer(name, clientAuthenticator, serverAuthenticator);
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
			using (var rep1 = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				rep1.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(1));

				var subject = new Mock<IGetDoubleProperty>();
				subject.Setup(x => x.Value).Returns(() =>
					{
						// We interrupt the connection from the calling endpoint itself
						rep1.Disconnect();
						return 42;
					});

				const int id = 1;
				server.CreateServant(id, subject.Object);
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
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(1));

				var subject = new Mock<IGetDoubleProperty>();
				subject.Setup(x => x.Value).Returns(() =>
					{
						// We interrupt the connection from the called endpoint
						server.Disconnect();
						return 42;
					});

				const int id = 1;
				server.CreateServant(id, subject.Object);
				var proxy = client.CreateProxy<IGetDoubleProperty>(id);

				new Action(() => { double unused = proxy.Value; })
					.ShouldThrow<ConnectionLostException>()
					.WithMessage("The connection to the remote endpoint has been lost");
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
		[Ignore("TODO: Find out why this test wont work - it should :P")]
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
				new Action(() => rep.Connect((IPEndPoint) null, TimeSpan.FromSeconds(1)))
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
		[Description("Verifies that Connect() succeeds when client-side authentication is enabled and the challenge is not met")]
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
		[Description("Verifies that Connect() succeeds when client side authentication is enabled but the client doesn't provide any")]
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
		[Description("Verifies that Connect() succeeds when server side authentication is enabled but the server doesn't provide any")]
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
		[Description("Verifies that a proxy on an unconnected endpoint can be created")]
		public void TestCreateProxy1()
		{
			using (var rep = CreateServer())
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
		[Description("Verifies that a proxy on an unconnected endpoint can be created")]
		public void TestCreateProxy2()
		{
			using (var rep = CreateClient())
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
			using (var endPoint = CreateServer())
			{
				var subject = new Mock<IEventInt32>();
				IServant servant = null;

				const string reason =
					"because a servant can always be created - its usage may however not work depending on the endpoint's connection status";
				new Action(() => servant = endPoint.CreateServant(0, subject.Object))
					.ShouldNotThrow(reason);
				servant.Should().NotBeNull(reason);

				new Action(() => subject.Raise(x => x.Foobar += null, 42))
					.ShouldThrow<NotConnectedException>(
						"because the endpoint is not connected to any other endpoint and thus there cannot be a proxy on which the event can ever be executed");
			}
		}

		[Test]
		[Description("Verifies that a servant on an unconnected endpoint can be created")]
		public void TestCreateServant2()
		{
			using (var endPoint = CreateClient())
			{
				var subject = new Mock<IEventInt32>();
				IServant servant = null;

				const string reason =
					"because a servant can always be created - its usage may however not work depending on the endpoint's connection status";
				new Action(() => servant = endPoint.CreateServant(0, subject.Object))
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
			using (var server = CreateServer())
			{
				server.Bind(IPAddress.Loopback);

				server.LocalEndPoint.Should().NotBeNull();
				server.LocalEndPoint.Address.Should().Be(IPAddress.Loopback);
				server.LocalEndPoint.Port.Should()
				   .BeInRange(49152, 65535,
				              "because an automatically chosen port should be in the range of private/dynamic port numbers");
				server.RemoteEndPoint.Should().BeNull();
				server.IsConnected.Should().BeFalse();
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect1()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);

				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				// Disconnecting from the endpoint that established the connection in the first place
				client.Disconnect();

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep2 is disconnected as well...
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that Disconnect() disconnects from the remote endpoint, sets the IsConnected property to false and the RemoteEndPoint property to null"
			)]
		public void TestDisconnect2()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				// Disconnecting from the other endpoint
				server.Disconnect();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// Unfortunately, for now, Disconnect() does not wait for approval of the remot endpoint and therefore we can't
				// immediately assert that rep1 is disconnected as well...
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that disconnecting and connecting to the same endpoint again is possible")]
		public void TestDisconnect3()
		{
			using (var client = CreateClient("Rep#1"))
			using (var server = CreateServer("Rep#2"))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(5));
				client.IsConnected.Should().BeTrue();
				server.IsConnected.Should().BeTrue();

				client.Disconnect();
				client.IsConnected.Should().BeFalse();
				WaitFor(() => !server.IsConnected, TimeSpan.FromSeconds(2))
					.Should().BeTrue();

				new Action(() => client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(1)))
					.ShouldNotThrow();
				client.IsConnected.Should().BeTrue();
				server.IsConnected.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that disposing the endpoint actually closes the listening socket")]
		public void TestDispose()
		{
			IPEndPoint endpoint;
			using (var ep = CreateServer("Foo"))
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

		[Test]
		[LocalTest("Wont run on the shitty CI server")]
		[Description("Verifies that creating automatic proxies & servants from both the client & server side don't cause grain-id collisions")]
		public void TestCreateAutomaticProxyAndServant()
		{
			using (var server = CreateServer())
			using (var client = CreateClient())
			{
				var foo1 = new Mock<IVoidMethodObjectParameter>();
				var foo1Listeners = new List<ulong>();
				foo1.Setup(x => x.AddListener(It.IsAny<object>()))
					.Callback((object x) => foo1Listeners.Add(((IGrain)x).ObjectId));

				server.CreateServant(0, foo1.Object);
				var proxy1 = client.CreateProxy<IVoidMethodObjectParameter>(0);

				var foo2 = new Mock<IVoidMethodObjectParameter>();
				var foo2Listeners = new List<ulong>();
				foo2.Setup(x => x.AddListener(It.IsAny<object>()))
					.Callback((object x) => foo2Listeners.Add(((IGrain)x).ObjectId));

				client.CreateServant(1, foo2.Object);
				var proxy2 = server.CreateProxy<IVoidMethodObjectParameter>(1);

				server.Bind(new IPEndPoint(IPAddress.Loopback, 56783));
				client.Connect(server.LocalEndPoint, TimeSpan.FromSeconds(10));

				const int numListeners = 1000;
				var task1 = new Task(() =>
					{
						for (int i = 0; i < numListeners; ++i)
						{
							var listener1 = new Mock<IByReferenceType>();
							proxy1.AddListener(listener1.Object);
						}
					});

				var task2 = new Task(() =>
					{
						for (int i = 0; i < numListeners; ++i)
						{
							var listener2 = new Mock<IVoidMethodStringParameter>();
							proxy2.AddListener(listener2.Object);
						}
					});

				task1.Start();
				task2.Start();
				Task.WaitAll(task1, task2);

				foo1Listeners.Count.Should().Be(numListeners);
				foo2Listeners.Count.Should().Be(numListeners);
				var intersection = (foo1Listeners.Intersect(foo2Listeners)).ToList();
				intersection.Should().BeEmpty("Because both client & server should've used completely different ids to identify all newly created servants & proxies");

				Console.WriteLine("{0}, {1}", foo1.Object, foo2.Object);
			}
		}

		[Test]
		[LocalTest("I'll switch the CI server really soon...")]
		[Description("Verifies that a serial RPC that in itself spawns another task doesn't deadlock")]
		public void TestSerialInvocationWithInnerTask()
		{
			using (var server = CreateServer())
			using (var client = CreateClient())
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint);

				var subject = new Mock<IInvokeAttributeMethods>();
				subject.Setup(x => x.SerializePerObject1()).Callback(() =>
				{
					var task = new Task<int>(() => 9001);
					task.Start();
					task.Wait();
				});

				server.CreateServant(0, subject.Object);
				var proxy = client.CreateProxy<IInvokeAttributeMethods>(0);

				Task.Factory.StartNew(proxy.SerializePerObject1)
					.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue("Because the method should've executed within 5 seconds");
			}
		}

		[Test]
		[LocalTest("Timing critical tests won't run on the C/I server")]
		[Description("Verifies that once a proxy-object is cleaned up by the garbage collector, it is automatically removed from the list of proxies")]
		public void TestGarbageCollection1()
		{
			using (var server = CreateServer())
			{
				//
				// Let's create a proxy and then verify that the proxy's alive for as long as it's referenced
				new Action(() =>
				{
					var proxy = server.CreateProxy<IGetFloatProperty>(42);
					server.Proxies.Contains((IProxy)proxy)
						  .Should()
						  .BeTrue("Because the proxy hasn't gone out of scope and thus will never be collected");
					server.NumProxiesCollected.Should().Be(0);
				})();

				GC.Collect(2, GCCollectionMode.Forced);
				GC.WaitForPendingFinalizers();

				// EndPoint's GC collects every 100 ms, thus waiting an additional 201ms
				// should have the internal GC collect at least once
				Thread.Sleep(201);

				// After having forced a collection, the proxy is reclaimed by the GC and thus
				// the list of proxie's should be empty.
				server.Proxies.Should().BeEmpty();
				server.NumProxiesCollected.Should().Be(1);
				server.GarbageCollectionTime.Should().BeLessThan(TimeSpan.FromMilliseconds(2));
			}
		}

		[Test]
		[LocalTest("Timing critical tests won't run on the C/I server")]
		public void TestGarbageCollection2()
		{
			using (var server = CreateServer())
			{
				//
				// Let's create a proxy and then verify that the proxy's alive for as long as it's referenced
				new Action(() =>
				{
					var proxy = server.CreateProxy<IGetFloatProperty>(42);
					server.Proxies.Contains((IProxy)proxy)
						  .Should()
						  .BeTrue("Because the proxy hasn't gone out of scope and thus will never be collected");
					server.NumProxiesCollected.Should().Be(0);
				})();

				GC.Collect(2, GCCollectionMode.Forced);
				GC.WaitForPendingFinalizers();

				// EndPoint's GC collects every 100 ms, thus we'll wait a little bit longer than that
				Thread.Sleep(201);

				// After having forced a collection, the proxy is reclaimed by the GC and thus
				// the list of proxie's should be empty.
				server.Proxies.Should().BeEmpty();
				server.NumProxiesCollected.Should().Be(1);
				server.GarbageCollectionTime.Should().BeLessThan(TimeSpan.FromMilliseconds(2));
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("Timing critical tests won't run on the C/I server")]
		[Description("Verifies that retrieving a proxy that no longer exists, but hasn't been garbage collected by the remoting endpoint, can be re-created")]
		public void TestGarbageCollection3()
		{
			using (var server = new SocketRemotingEndPointServer())
			{
				new Action(() =>
				{
					var proxy = server.CreateProxy<IGetFloatProperty>(42);
					server.Proxies.Contains((IProxy)proxy)
						  .Should()
						  .BeTrue("Because the proxy hasn't gone out of scope and thus will never be collected");
				})();

				GC.Collect(2, GCCollectionMode.Forced);

				// After having forced a collection, the proxy is reclaimed by the GC and thus
				// the list of proxie's should be empty.
				server.Proxies.Should().BeEmpty();
				var actualProxy = server.GetExistingOrCreateNewProxy<IGetFloatProperty>(42);
				actualProxy.Should().NotBeNull();
			}
		}
	}
}