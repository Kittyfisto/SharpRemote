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
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	[TestFixture]
	public class Test
		: AbstractTest
	{
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
		[Description("Verifies that disposing the endpoint actually closes the listening socket")]
		public void TestDispose1()
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
				// the list of proxie's should be empty (besides the ILatency and IHeartbeat interface
				// which are always installed on each endpoint).
				server.Proxies.Count().Should().Be(2);
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
				// the list of proxie's should be empty (besides the ILatency and IHeartbeat interface
				// which are always installed on each endpoint).
				server.Proxies.Count().Should().Be(2);
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
				// the list of proxie's should be empty (besides the ILatency and IHeartbeat interface
				// which are always installed on each endpoint).
				server.Proxies.Count().Should().Be(2);
				var actualProxy = server.GetExistingOrCreateNewProxy<IGetFloatProperty>(42);
				actualProxy.Should().NotBeNull();
			}
		}
	}
}