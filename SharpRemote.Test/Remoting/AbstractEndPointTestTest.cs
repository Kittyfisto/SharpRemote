using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Remoting
{
	public abstract class AbstractEndPointTestTest
		: AbstractEndPointTest
	{
		protected abstract void Connect(IRemotingEndPoint rep1, EndPoint localEndPoint);
		protected abstract void Connect(IRemotingEndPoint rep1, EndPoint localEndPoint, TimeSpan timeout);

		protected abstract EndPoint EndPoint1 { get; }

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description(
			"Verifies that when the connection between two endpoints is interrupted from the calling end, any ongoing synchronous method call is stopped and an exception is thrown on the calling thread"
			)]
		public void TestCallMethod1()
		{
			using (var rep1 = CreateClient(name: "Rep#1"))
			using (var server = CreateServer(name: "Rep#2"))
			{
				Bind(server);
				Connect(rep1, server.LocalEndPoint, TimeSpan.FromSeconds(1));

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
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description(
			"Verifies that when the connection between two endpoints is interrupted from the called end, any ongoing synchronous method call is stopped and an exception is thrown on the calling thread"
			)]
		public void TestCallMethod2()
		{
			using (var client = CreateClient(name: "Rep#1"))
			using (var server = CreateServer(name: "Rep#2"))
			{
				Bind(server);
				Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(1));

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
		[Description("Verifies that if a matching servant has been registered with the endpoint, then RetrieveSubject simply returns the original subject")]
		public void TestRegisterServant3()
		{
			using (var server = CreateServer())
			{
				var subject = new Mock<IByReferenceType>().Object;
				server.CreateServant(9001, subject);

				var proxy = server.RetrieveSubject<IByReferenceType>(9001);
				proxy.Should().NotBeNull();
				proxy.Should().BeSameAs(subject, "because we've just registered a subject with that id");
			}
		}

		[Test]
		[Description("Verifies that if no matching servant has been registered with the endpoint, then RetrieveSubject simply returns null")]
		public void TestRegisterServant4()
		{
			using (var server = CreateServer())
			{
				var subject = new Mock<IByReferenceType>().Object;
				server.CreateServant(9001, subject);

				var proxy = server.RetrieveSubject<IByReferenceType>(9002);
				proxy.Should().BeNull("because we didn't register a subject with the given id");
			}
		}

		[Test]
		[Description("Verifies that if no matching servant has been registered with the endpoint, then RetrieveSubject simply returns null")]
		public void TestRegisterServant5()
		{
			using (var server = CreateServer())
			{
				var subject = new Mock<IByReferenceType>().Object;
				server.CreateServant(9001, subject);

				var proxy = server.RetrieveSubject<IByReferenceReturnMethodInterface>(9001);
				proxy.Should().BeNull("because we registered the subject under a different (incompatible) type");
			}
		}

		[Test]
		[Description("Verifies that the endpoint settings are properly forwarded")]
		public void TestCtor2()
		{
			var endPointSettings = new EndPointSettings { MaxConcurrentCalls = 42 };
			using (var server = CreateServer(endPointSettings: endPointSettings))
			{
				server.EndPointSettings.Should().NotBeNull();
				server.EndPointSettings.MaxConcurrentCalls.Should().Be(42);
			}
		}

		[Test]
		[Description("Verifies that the current connection id is set to none after construction")]
		public void TestCtor3()
		{
			using (var client = CreateClient())
			using (var server = CreateServer())
			{
				client.CurrentConnectionId.Should().Be(ConnectionId.None);
				server.CurrentConnectionId.Should().Be(ConnectionId.None);
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
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

				Bind(server, EndPoint1);
				Connect(client, server.LocalEndPoint, TimeSpan.FromSeconds(10));

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
				Bind(server);
				Connect(client, server.LocalEndPoint);

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
		[Description("Verifies that SharpRemote manages situations in which a proxy of a subject is passed back to the endpoint which holds the subject")]
		public void TestByReference()
		{
			using (var server = CreateServer())
			using (var client = CreateClient())
			{
				Bind(server);
				Connect(client, server.LocalEndPoint);

				var factory = new Mock<IFactory>();
				var @object = new Mock<IByReferenceType>().Object;
				factory.Setup(x => x.Create()).Returns(@object);

				server.CreateServant(42, factory.Object);
				var factoryProxy = client.CreateProxy<IFactory>(42);
				var actualObject = factoryProxy.Create();
				actualObject.Should().NotBeNull();

				factoryProxy.Remove(actualObject);
				// We've marked IByReferenceType with the ByReferenceAttribute and thus we expect that
				// if we pass a proxy to a method, then the original factory receives the *original* object
				// and not a proxy to a proxy. The following test makes sure that this is actually the case.
				factory.Verify(x => x.Remove(It.Is<IByReferenceType>(y => ReferenceEquals(y, @object))),
					Times.Once, "because the original object should've been passed to the factory (and not the proxy to a proxy)");
			}
		}

		[Test]
		[Description("Verifies that GetOrCreateProxy creates a new proxy-object if none existed before")]
		public void TestGetOrCreateProxy1()
		{
			using (var server = CreateServer())
			{
				var proxy = server.GetExistingOrCreateNewProxy<IByReferenceType>(9001);
				proxy.Should().NotBeNull("because a new proxy should've been created");
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
				server.TotalGarbageCollectionTime.Should().BeLessThan(TimeSpan.FromMilliseconds(2));
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
				server.TotalGarbageCollectionTime.Should().BeLessThan(TimeSpan.FromMilliseconds(2));
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("Timing critical tests won't run on the C/I server")]
		[Description("Verifies that retrieving a proxy that no longer exists, but hasn't been garbage collected by the remoting endpoint, can be re-created")]
		public void TestGarbageCollection3()
		{
			using (var server = new SocketEndPoint(EndPointType.Server))
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

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that when the connection is dropped because of a connection-failure, the OnFailure event is invoked")]
		public void TestOnFailure1()
		{
			using (var server = new SocketEndPoint(EndPointType.Server))
			using (var client = new SocketEndPoint(EndPointType.Client))
			{
				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint);

				EndPointDisconnectReason? reason = null;
				client.OnFailure += (r, unused) => reason = r;

				server.DisconnectByFailure();
				WaitFor(() => reason != null, TimeSpan.FromSeconds(1))
					.Should().BeTrue("Because the client should've detected and reported the failure");
			}
		}

		[Test]
		[LocalTest("Why does this test keep failing on AppVeyor? Nobody knows why...")]
		[Description("Verifies that invoking a method on a proxy from inside OnFailure doesn't cause a deadlock")]
		public void TestOnFailure2()
		{
			using (var server = new SocketEndPoint(EndPointType.Server))
			using (var client = new SocketEndPoint(EndPointType.Client))
			{
				var subject = new Mock<IVoidMethod>();
				server.CreateServant(42, subject.Object);
				var proxy = client.CreateProxy<IVoidMethod>(42);

				server.Bind(IPAddress.Loopback);
				client.Connect(server.LocalEndPoint);

				bool failed = false;
				client.OnFailure += (r, unused) =>
				{
					new Action(proxy.DoStuff)
						.ShouldThrow<NotConnectedException>();
					failed = true;
				};

				server.DisconnectByFailure();
				WaitFor(() => failed, TimeSpan.FromSeconds(2))
					.Should().BeTrue("Because the client should've detected and reported the failure");
			}
		}
	}
}