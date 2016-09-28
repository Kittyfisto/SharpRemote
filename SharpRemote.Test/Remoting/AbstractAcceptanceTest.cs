using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Exceptions;
using SharpRemote.Extensions;
using SharpRemote.Hosting;
using SharpRemote.Test.Types;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Exceptions;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using SharpRemote.Test.Types.Structs;
using log4net.Core;

namespace SharpRemote.Test.Remoting
{
	public abstract class AbstractAcceptanceTest
		: AbstractTest
	{
		private IRemotingEndPoint _client;
		private IRemotingEndPoint _server;

		[TestFixtureSetUp]
		public new void SetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<AbstractEndPoint>(Level.Info);
			TestLogger.SetLevel<AbstractIPSocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointClient>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointServer>(Level.Info);

			_server = CreateServer();
			Bind(_server);

			_client = CreateClient();
			Connect(_client, _server);
		}

		protected abstract IRemotingEndPoint CreateServer();
		protected abstract void Bind(IRemotingEndPoint server);

		protected abstract IRemotingEndPoint CreateClient();
		protected abstract void Connect(IRemotingEndPoint client, IRemotingEndPoint server);

		[TestFixtureTearDown]
		public void TearDown()
		{
			TestLogger.DisableConsoleLogging();

			_server.TryDispose();
			_client.TryDispose();
		}

		[Test]
		[NUnit.Framework.Description("Verifies (since overloaded methods are not yet supported) that types which contains two or more methods of the same name are not supported and an appropriate exception is thrown")]
		public void TestTypeWithOverloadedMethods()
		{
			const int servantId = 50;
			new Action(() => _server.CreateProxy<IOverloadedMethods>(servantId))
				.ShouldThrow<ArgumentException>()
				.WithMessage("Unable to create proxy for type 'IOverloadedMethods': The type contains at least two methods with the same name 'Print': This is not supported");

			new Action(() => _server.CreateServant(servantId, new Mock<IOverloadedMethods>().Object))
				.ShouldThrow<ArgumentException>()
				.WithMessage("Unable to create servant for type 'IOverloadedMethods': The type contains at least two methods with the same name 'Print': This is not supported");
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that synchronous methods are executed in the order they are issued in - by issuing them from one thread")]
		public void TestCallOrder1()
		{
			var subject = new OrderedInterface();
			const int servantId = 18;
			_server.CreateServant(servantId, (IOrderInterface) subject);
			var proxy = _client.CreateProxy<IOrderInterface>(servantId);

			for (int i = 0; i < 1000; ++i)
			{
				proxy.Unordered(i);
			}

			subject.UnorderedSequence.Should().Equal(
				Enumerable.Range(0, 1000).ToList(),
				"Because synchronous calls made from the same thread are guarantueed to be executed in order"
				);
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that synchronous methods are executed in the order they are issued in - by issuing them from two threads")]
		public void TestCallOrder2()
		{
			var subject = new OrderedInterface();
			const int servantId = 19;
			_server.CreateServant(servantId, (IOrderInterface) subject);
			var proxy = _client.CreateProxy<IOrderInterface>(servantId);

			List<int> sequence = Enumerable.Range(0, 1000).ToList();
			Action fn = () =>
				{
					while (true)
					{
						lock (sequence)
						{
							if (sequence.Count == 0)
								break;

							int next = sequence[0];
							sequence.RemoveAt(0);
							proxy.Unordered(next);
						}
					}
				};
			var tasks = new[]
				{
					Task.Factory.StartNew(fn),
					Task.Factory.StartNew(fn)
				};
			Task.WaitAll(tasks);

			subject.UnorderedSequence.Should().Equal(
				Enumerable.Range(0, 1000).ToList(),
				"Because synchronous calls are executed in the same sequence they are given, independent of the calling thread"
				);
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that invocations on a SerializePerType method are serialized, even when the calls happen in parallel")]
		public void TestCallOrder3()
		{
			var subject = new OrderedInterface();
			const int servantId = 20;
			_server.CreateServant(servantId, (IOrderInterface) subject);
			var proxy = _client.CreateProxy<IOrderInterface>(servantId);

			List<int> sequence = Enumerable.Range(0, 1000).ToList();
			Action fn = () =>
				{
					while (true)
					{
						int next;
						lock (sequence)
						{
							if (sequence.Count == 0)
								break;

							next = sequence[0];
							sequence.RemoveAt(0);
						}

						proxy.TypeOrdered(next);
					}
				};
			var tasks = new[]
				{
					Task.Factory.StartNew(fn),
					Task.Factory.StartNew(fn),
					Task.Factory.StartNew(fn),
					Task.Factory.StartNew(fn),
					Task.Factory.StartNew(fn),
					Task.Factory.StartNew(fn)
				};
			Task.WaitAll(tasks);

			subject.TypeOrderedSequence.Should().BeEquivalentTo(
				Enumerable.Range(0, 1000).ToList(),
				"Because the proxy calls are not necessarily ordered, its invocations aren't either."
				);
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that when a serialize by type method is invoked, the calling thread's name is set to the full name of the interface (to allow for better debugging)"
			)]
		public void TestCallOrder4()
		{
			Thread thread = null;

			var subject = new Mock<IOrderInterface>();
			subject.Setup(x => x.TypeOrdered(It.IsAny<int>()))
			       .Callback(() => { thread = Thread.CurrentThread; });

			const int servantId = 21;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IOrderInterface>(servantId);

			proxy.TypeOrdered(42);
			thread.Should().NotBeNull();
			thread.Name.Should().Be("SharpRemote.Test.Types.Interfaces.IOrderInterface");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that when a serialize by object method is invoked, the calling thread's name is set to the full name of the interface plus the grain id (to allow for better debugging)"
			)]
		public void TestCallOrder5()
		{
			Thread thread = null;

			var subject = new Mock<IOrderInterface>();
			subject.Setup(x => x.InstanceOrdered(It.IsAny<int>()))
			       .Callback(() => { thread = Thread.CurrentThread; });

			const int servantId = 22;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IOrderInterface>(servantId);

			proxy.InstanceOrdered(42);
			thread.Should().NotBeNull();
			thread.Name.Should().Be("SharpRemote.Test.Types.Interfaces.IOrderInterface (#22)");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that when a serialize by object method is invoked, the calling thread's name is set to the full name of the interface plus the grain id (to allow for better debugging)"
			)]
		public void TestCallOrder6()
		{
			Thread thread = null;

			var subject = new Mock<IOrderInterface>();
			subject.Setup(x => x.MethodOrdered(It.IsAny<int>()))
			       .Callback(() => { thread = Thread.CurrentThread; });

			const int servantId = 23;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IOrderInterface>(servantId);

			proxy.MethodOrdered(42);
			thread.Should().NotBeNull();
			thread.Name.Should().Be("SharpRemote.Test.Types.Interfaces.IOrderInterface.MethodOrdered() (#23)");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		public void TestCreateSubject()
		{
			var subject = new Mock<ISubjectHost>();

			ulong? actualId = null;
			Type type = null;
			Type @interface = null;

			subject.Setup(x => x.CreateSubject1(It.IsAny<ulong>(), It.IsAny<Type>(), It.IsAny<Type>()))
			       .Callback((ulong id, Type a, Type b) =>
				       {
					       actualId = id;
					       type = a;
					       @interface = b;
				       });

			const int servantId = 10;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<ISubjectHost>(servantId);

			proxy.CreateSubject1(42, typeof (GetStringPropertyImplementation), typeof (IGetStringProperty));
			actualId.Should().Be(42ul);
			type.Should().Be<GetStringPropertyImplementation>();
			@interface.Should().Be<IGetStringProperty>();
		}

		[Test]
		[NUnit.Framework.Description("")]
		public void TestGetNonStartedTaskIsNotSupported1()
		{
			const int servantId = 14;
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => new Task(() => { }));
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsTask>(servantId);
			new Action(() => proxy.DoStuff().Wait())
				.ShouldThrow<NotSupportedException>()
				.WithMessage("IReturnsTask.DoStuff of servant #14 returned a non-started task - this is not supported");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("")]
		public void TestGetNonStartedTaskIsNotSupported2()
		{
			const int servantId = 15;
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => new Task<int>(() => 42));
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsIntTask>(servantId);
			new Action(() => proxy.DoStuff().Wait())
				.ShouldThrow<NotSupportedException>()
				.WithMessage("IReturnsIntTask.DoStuff of servant #15 returned a non-started task - this is not supported");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(42);

			const int servantId = 1;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			proxy.Value.Should().Be(42);

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that an eception can be marshalled")]
		public void TestGetPropertyThrowException1()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(() => { throw new ArgumentException("Foobar"); });

			const int servantId = 2;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			new Action(() => { double unused = proxy.Value; })
				.ShouldThrow<ArgumentException>()
				.WithMessage("Foobar");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that if an exception could not be serialized, but can be re-constructed due to a default ctor, then it is thrown again"
			)]
		public void TestGetPropertyThrowNonSerializableException()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(() => { throw new NonSerializableExceptionButDefaultCtor(); });

			const int servantId = 3;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			new Action(() => { double unused = proxy.Value; })
				.ShouldThrow<UnserializableException>();

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		public void TestGetTaskContinueWith()
		{
			const int servantId = 13;
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => Task<int>.Factory.StartNew(() => 42));
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsIntTask>(servantId);
			int? result = null;
			Task task = proxy.DoStuff().ContinueWith(unused => { result = unused.Result; });
			task.Wait();
			result.Should().Be(42);

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that the exception thrown by a task is correctly marshalled")]
		public void TestGetTaskThrowException1()
		{
			const int servantId = 11;
			var subject = new Mock<IReturnsTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => Task.Factory.StartNew(() => { throw new Win32Exception(1337); }));
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsTask>(servantId);
			Task task = proxy.DoStuff();
			new Action(task.Wait)
				.ShouldThrow<AggregateException>();

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that the exception thrown by a task is correctly marshalled")]
		public void TestGetTaskThrowException2()
		{
			const int servantId = 12;
			var subject = new Mock<IReturnsIntTask>();
			subject.Setup(x => x.DoStuff()).Returns(() => Task<int>.Factory.StartNew(() => { throw new Win32Exception(1337); }));
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsIntTask>(servantId);
			Task<int> task = proxy.DoStuff();
			new Action(task.Wait)
				.ShouldThrow<AggregateException>();

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that creating a proxy with the wrong type doesn't throw")]
		public void TestInterfaceTypeMismatch1()
		{
			var subject = new Mock<IReturnsIntTask>();
			const int objectId = 16;
			_server.CreateServant(objectId, subject.Object);
			new Action(() => _client.CreateProxy<IReturnsTask>(objectId))
				.ShouldNotThrow("Because creating proxy & servant of different type is not wrong, until a method is invoked");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that invoking a method on a proxy/servant type mismatch throws")]
		public void TestInterfaceTypeMismatch2()
		{
			var subject = new Mock<IReturnsIntTask>();
			const int objectId = 17;
			_server.CreateServant(objectId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsTask>(objectId);
			new Action(() => proxy.DoStuff().Wait())
				.ShouldThrow<TypeMismatchException>();

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that an interface which itself implements another interface works")]
		public void TestMultipleInterfaces()
		{
			var subject = new Mock<ICalculator>();
			bool isDisposed = false;
			subject.Setup(x => x.IsDisposed).Returns(() => isDisposed);
			subject.Setup(x => x.Dispose()).Callback(() => isDisposed = true);
			subject.Setup(x => x.Add(It.IsAny<double>(), It.IsAny<double>()))
			       .Returns((double x, double y) => x + y);

			const int servantId = 9;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<ICalculator>(servantId);

			proxy.Add(1, 2).Should().Be(3);
			proxy.Add(5, 42).Should().Be(47);
			proxy.IsDisposed.Should().BeFalse();
			proxy.Dispose();
			proxy.IsDisposed.Should().BeTrue();

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that raising an event on the subject to which no-one is connected via the proxy doesn't do anything - besides not failing"
			)]
		public void TestRaiseEmptyEvent()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 4;
			IServant servant = _server.CreateServant(servantId, subject.Object);
			var proxy = (IProxy) _client.CreateProxy<IEventInt32>(servantId);

			new Action(() => subject.Raise(x => x.Foobar += null, 42))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));

			servant.ObjectId.Should().Be(servantId);
			proxy.ObjectId.Should().Be(servantId);

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that raising an event on the subject successfully serialized the parameter's value and forwards it to the proxy"
			)]
		public void TestRaiseEvent1()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 5;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			int? actualValue = null;
			proxy.Foobar += x => actualValue = x;

			const int value = 42;
			new Action(() => subject.Raise(x => x.Foobar += null, value))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));

			actualValue.Should().Be(value);

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that delegates are invoked in the exact order that they are registered in")]
		public void TestRaiseEvent2()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 6;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			int? actualValue1 = null;
			int? actualValue2 = null;
			proxy.Foobar += x => actualValue1 = x;
			proxy.Foobar += x => { throw new ArgumentOutOfRangeException("value"); };
			proxy.Foobar += x => actualValue2 = x;

			const int value = 42;
			new Action(() => subject.Raise(x => x.Foobar += null, value))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: value");

			actualValue1.Should().Be(value);
			actualValue2.Should().NotHaveValue();

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that a delegate is no longer invoked once it's removed from the event")]
		public void TestRaiseEvent3()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 7;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			int? actualValue = null;
			Action<int> fn = x => actualValue = x;
			proxy.Foobar += fn;

			const int value1 = 9001;
			new Action(() => subject.Raise(x => x.Foobar += null, value1))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));
			actualValue.Should().Be(value1);

			proxy.Foobar -= fn;
			const int value2 = int.MaxValue;
			new Action(() => subject.Raise(x => x.Foobar += null, value2))
				.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(1));
			actualValue.Should().Be(value1);

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that an exception is successfully marshalled when thrown by the delegate attached to the proxie's event")]
		public void TestRaiseEventThrowException1()
		{
			var subject = new Mock<IEventInt32>();
			const int servantId = 8;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IEventInt32>(servantId);

			proxy.Foobar += x => { throw new ArgumentOutOfRangeException("value"); };

			const int value = 42;
			new Action(() => subject.Raise(x => x.Foobar += null, value))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: value");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that a method accepting a by reference parameter can be called and that the appropriate proxies & servants are created"
			)]
		public void TestAddByReference1()
		{
			const ulong servantId = 30;
			var processor = new Processor();
			processor.Listeners.Should().BeEmpty();

			_server.CreateServant(servantId, (IProcessor) processor);
			var proxy = _client.CreateProxy<IProcessor>(servantId);

			var listener1 = new Listener();
			proxy.AddListener(listener1);
			IServant listenerServant = Servants(_client).First(x => x.InterfaceType == typeof (IListener));
			IProxy listenerProxy = _server.Proxies.First(x => x.InterfaceType == typeof (IListener));
			listenerServant.ObjectId.Should()
			               .Be(listenerProxy.ObjectId,
			                   "Because both proxy and servant must have been created for that specific listener instance because of the remote method call");


			processor.Listeners.Count.Should().Be(1, "Because AddListener() should have added a listener to the processor");
			processor.Listeners[0].Should()
			                      .NotBe(listener1, "Because not the real listener has been added, but a proxy representing it");
			processor.Listeners[0].Should()
			                      .BeSameAs(listenerProxy,
			                                "Because not the real listener has been added, but a proxy representing it");


			proxy.Report("Foobar");
			listener1.Messages.Should()
			         .Equal(new[] {"Foobar"},
			                "Because Processor.Report() method should have invoked the Report() method on the proxy which in turn would've called the real listener implementation");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			processor.Should().NotBeNull();
		}

		protected abstract IEnumerable<IServant> Servants(IRemotingEndPoint client);

		[Test]
		[NUnit.Framework.Description(
			"Verifies that successive calls with a [ByReference] parameter are marshalled using the same proxy instance on the invoked end, mimicking these calls in a non-RPC scenario"
			)]
		public void TestAddByReference2()
		{
			const ulong servantId = 31;
			var processor = new Processor();
			processor.Listeners.Should().BeEmpty();

			_server.CreateServant(servantId, (IProcessor) processor);
			var proxy = _client.CreateProxy<IProcessor>(servantId);

			var listener = new Listener();
			proxy.AddListener(listener);
			proxy.AddListener(listener);

			processor.Listeners.Count.Should().Be(2);
			processor.Listeners[0].Should()
			                      .BeSameAs(processor.Listeners[1],
			                                "Because we passed the same [ByReference] object to AddListener and thus the same proxy object should've been given to IProcessor.AddListener()");
			processor.Listeners[0].Should().NotBeSameAs(listener, "Because a proxy must have been added, not the real listener");
		}

		[Test]
		[NUnit.Framework.Description("Verifies that creating proxies & servants for serialized objects is thread safe")]
		public void TestAddByReference3()
		{
			const ulong servantId = 32;
			var processor = new Processor();
			processor.Listeners.Should().BeEmpty();

			_server.CreateServant(servantId, (IProcessor) processor);
			var proxy = _client.CreateProxy<IProcessor>(servantId);

			const int numTasks = 16;
			const int numListenersPerTask = 100;

			Console.WriteLine("Adding {0} listeners in each of {1} parallel tasks",
			                  numListenersPerTask,
			                  numTasks);
			var sw = new Stopwatch();
			sw.Start();

			var tasks = new Task[numTasks];
			for (int i = 0; i < numTasks; ++i)
			{
				tasks[i] = new Task(() =>
					{
						for (int x = 0; x < numListenersPerTask; ++x)
						{
							var listener = new Listener();
							proxy.AddListener(listener);
							proxy.AddListener(listener);
						}
					});
				tasks[i].Start();
			}
			Task.WaitAll(tasks);

			sw.Stop();
			Console.WriteLine("Took {0}ms", sw.ElapsedMilliseconds);

			const int numUniqueListeners = numTasks*numListenersPerTask;
			processor.Listeners.Count.Should().Be(numUniqueListeners*2, "Because each unique listener instance is added twice");
		}

		[Test]
		[NUnit.Framework.Description("Verifies that creating proxies & servants for serialized objects is thread safe")]
		public void TestAddByReference4()
		{
			const ulong servantId = 24;
			var processor = new Processor();
			processor.Listeners.Should().BeEmpty();

			_server.CreateServant(servantId, (IProcessor) processor);
			var proxy = _client.CreateProxy<IProcessor>(servantId);

			const int numTasks = 16;
			const int numListenersPerTask = 100;

			Console.WriteLine("Adding {0} listeners in each of {1} parallel tasks",
			                  numListenersPerTask,
			                  numTasks);
			var sw = new Stopwatch();
			sw.Start();

			var listener = new Listener();
			var tasks = new Task[numTasks];
			for (int i = 0; i < numTasks; ++i)
			{
				tasks[i] = new Task(() =>
					{
						for (int x = 0; x < numListenersPerTask; ++x)
						{
							proxy.AddListener(listener);
						}
					});
				tasks[i].Start();
			}
			Task.WaitAll(tasks);

			sw.Stop();
			Console.WriteLine("Took {0}ms", sw.ElapsedMilliseconds);

			const int numUniqueListeners = numTasks*numListenersPerTask;
			processor.Listeners.Count.Should().Be(numUniqueListeners);
		}


		[Test]
		[NUnit.Framework.Description(
			"Verifies that a [ByReference] field of a struct that's passed as an object is correctly deserialized on the other end"
			)]
		public void TestAddObjectByReference1()
		{
			var reference = new ByReferenceClass(9001);
			var value = new FieldObjectStruct
				{
					Value = reference
				};

			var subject = new Mock<IVoidMethodObjectParameter>();
			object actualValue = null;
			subject.Setup(x => x.AddListener(It.IsAny<object>()))
			       .Callback((object x) => actualValue = x);

			const ulong servantId = 25;
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IVoidMethodObjectParameter>(servantId);

			proxy.AddListener(value);
			actualValue.Should().NotBeNull();
			actualValue.Should().BeOfType<FieldObjectStruct>();
			(((FieldObjectStruct) actualValue).Value is IByReferenceType).Should().BeTrue();
			object actualReference = ((FieldObjectStruct) actualValue).Value;

			proxy.AddListener(value);
			((FieldObjectStruct) actualValue).Value.Should()
			                                 .BeSameAs(actualReference,
			                                           "Because [ByReference] types should adhere to referential equality after deserialization");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that a method returning a [ByReference] type is marshalled correctly")]
		public void TestReturnByReference1()
		{
			const ulong servantId = 26;

			var subject = new Mock<IReturnsByReferenceType>();
			var foo = new ByReferenceClass();
			subject.Setup(x => x.GetFoo()).Returns(foo);

			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsByReferenceType>(servantId);

			IByReferenceType actualFoo1 = proxy.GetFoo();
			actualFoo1.Should().NotBeNull();
			actualFoo1.Value.Should().Be(foo.Value);

			proxy.GetFoo()
			     .Should()
			     .BeSameAs(actualFoo1,
			               "because [ByReference] types must be marshalled with referential equality in mind - GetFoo() always returns the same instance and thus the proxy should as well");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that a method returning an object of a [ByReference] type is marshalled correctly")]
		public void TestReturnByReference2()
		{
			const ulong servantId = 27;

			var subject = new Mock<IReturnsObjectMethod>();
			var foo = new ByReferenceClass();
			subject.Setup(x => x.GetListener()).Returns(foo);

			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsObjectMethod>(servantId);

			var actualFoo1 = proxy.GetListener() as IByReferenceType;
			actualFoo1.Should().NotBeNull();
			actualFoo1.Value.Should().Be(foo.Value);

			proxy.GetListener()
			     .Should()
			     .BeSameAs(actualFoo1,
			               "because [ByReference] types must be marshalled with referential equality in mind - GetFoo() always returns the same instance and thus the proxy should as well");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that a method returning a list of objects where some are a [ByReference] types is marshalled correctly")]
		public void TestReturnListOfByReferences()
		{
			const ulong servantId = 28;

			var subject = new Mock<IReturnsObjectArray>();
			var foo1 = new ByReferenceClass(42);
			var foo2 = new ByReferenceClass(9001);
			subject.Setup(x => x.Objects).Returns(new object[]
				{
					foo1,
					foo2,
					foo1,
					42,
					"Hello World!"
				});

			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IReturnsObjectArray>(servantId);

			object[] objects = proxy.Objects;
			objects.Should().NotBeNull();
			objects.Length.Should().Be(5);
			objects[0].Should().NotBeNull();
			(objects[0] is IByReferenceType).Should().BeTrue();
			((IByReferenceType) objects[0]).Value.Should().Be(foo1.Value);

			objects[1].Should().NotBeNull();
			(objects[1] is IByReferenceType).Should().BeTrue();
			((IByReferenceType) objects[1]).Value.Should().Be(foo2.Value);

			objects[2].Should().BeSameAs(objects[0]);
			objects[3].Should().Be(42);
			objects[4].Should().Be("Hello World!");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description("Verifies that transfering a string array is possible")]
		public void TestEventActionStringArray()
		{
			const ulong servantId = 39;

			var values = new List<string>();
			var subject = new Mock<IActionEventStringArray>();

			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IActionEventStringArray>(servantId);
			proxy.Do += values.AddRange;

			subject.Raise(x => x.Do += null, new object[] {new[]{"COM11", "COM13"}});

			values.Should().Equal(new object[]
				{
					"COM11",
					"COM13"
				});
		}

		[Test]
		[NUnit.Framework.Description("Verifies that transfering a string array is possible")]
		public void TestVoidMethodStringArray()
		{
			const ulong servantId = 37;

			var values = new List<string>();
			var subject = new Mock<IVoidMethodStringArrayParameter>();
			subject.Setup(x => x.Do(It.IsAny<string[]>()))
			       .Callback((string[] data) => values.AddRange(data));

			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IVoidMethodStringArrayParameter>(servantId);
			proxy.Do(new[]{"COM11", "COM13"});

			values.Should().Equal(new object[]
				{
					"COM11",
					"COM13"
				});
		}

		[Test]
		[NUnit.Framework.Description("Verifies that transfering a decimal value is possible")]
		public void TestGetDecimalProperty()
		{
			const ulong servantId = 38;

			var value = decimal.Parse("3.1415926535897932384626433832");
			var subject = new Mock<IGetDecimalProperty>();
			subject.Setup(x => x.Value).Returns(value);

			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDecimalProperty>(servantId);
			var actualValue = proxy.Value;
			actualValue.Should().Be(value);
		}

		[Test]
		[NUnit.Framework.Description("Verifies that a method applied with the Async attribute is truly invoked async")]
		public void TestAsyncAttribute1()
		{
			const ulong servantId = 29;
			var syncRoot = new object();

			string actualMessage = null;
			bool called = false;
			var subject = new Mock<IVoidMethodAsyncAttribute>();
			subject.Setup(x => x.Do(It.IsAny<string>()))
			       .Callback((string message) =>
				       {
					       lock (syncRoot)
					       {
						       actualMessage = message;
						       called = true;
					       }
				       });
			_server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IVoidMethodAsyncAttribute>(servantId);

			Task t = Task.Factory.StartNew(() =>
				{
					lock (syncRoot)
					{
						proxy.Do("Test");
					}
				});
			t.Wait(TimeSpan.FromSeconds(2))
			 .Should().BeTrue("Because this method shouldn't deadlock when executed asynchronously");

			WaitFor(() => called, TimeSpan.FromSeconds(1))
				.Should().BeTrue("Because the remote method should've been invoked well within one second");
			actualMessage.Should().Be("Test");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this shitty CI server")]
		[NUnit.Framework.Description(
			"Verifies that when a method applied with the Async attribute throws an exception, the exception is properly handled and doesn't tear down the entire process"
			)]
		public void TestAsyncAttribute2()
		{
			var exceptions = new List<Exception>();
			EventHandler<UnobservedTaskExceptionEventArgs> fn = (sender, args) => exceptions.Add(args.Exception);
			TaskScheduler.UnobservedTaskException += fn;
			try
			{
				const ulong servantId = 33;

				bool called = false;
				var subject = new Mock<IVoidMethodAsyncAttribute>();
				subject.Setup(x => x.Do(It.IsAny<string>()))
				       .Callback((string message) =>
					       {
						       called = true;
						       throw new ArgumentException("Foobar");
					       });
				_server.CreateServant(servantId, subject.Object);
				var proxy = _client.CreateProxy<IVoidMethodAsyncAttribute>(servantId);

				proxy.Do("Test");

				WaitFor(() => called, TimeSpan.FromSeconds(1))
					.Should().BeTrue("Because the remote method should've been invoked well within one second");

				GC.Collect(2, GCCollectionMode.Forced);

				exceptions.Should().BeEmpty();

				// This line exists to FORCE the GC to NOT collect the subject, which
				// in turn would unregister the servant from the server, thus making the test
				// fail.
				subject.Should().NotBeNull();
			}
			finally
			{
				TaskScheduler.UnobservedTaskException -= fn;
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that an event applied with the Async attribute is truly invoked async")]
		public void TestAsyncAttribute3()
		{
			const ulong servantId = 36;
			var syncRoot = new object();

			string actualMessage = null;
			bool called = false;
			var proxy = _client.CreateProxy<IInvokeAttributeEvents>(servantId);
			proxy.Async1 += message =>
			{
				lock (syncRoot)
				{
					actualMessage = message;
					called = true;
				}
			};
			var subject = new Mock<IInvokeAttributeEvents>();
			_server.CreateServant(servantId, subject.Object);
			Task t = Task.Factory.StartNew(() =>
			{
				lock (syncRoot)
				{
					subject.Raise(x => x.Async1 += null, "Foobar");
				}
			});
			t.Wait(TimeSpan.FromSeconds(20))
			 .Should().BeTrue("Because the event invocation shouldn't deadlock when executed asynchronously");

			WaitFor(() => called, TimeSpan.FromSeconds(10))
				.Should().BeTrue("Because the event handler should've been invoked well within one second");
			actualMessage.Should().Be("Foobar");

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that when a method is invoked on a proxy where no corresponding servant is registered with the other EndPoint, then an appropriate exception is thrown"
			)]
		public void TestNoSuchServant()
		{
			const ulong servantId = 34;
			var proxy = _client.CreateProxy<IInt32Method>(servantId);
			new Action(() => proxy.Do())
				.ShouldThrow<NoSuchServantException>();

			const string reason =
				"Because a method invocation on a non-existant servant should not be a reason to tear down the connection";
			_client.IsConnected.Should().BeTrue(reason);
			_server.IsConnected.Should().BeTrue(reason);
		}

		[Test]
		[NUnit.Framework.Description(
			"Verifies that when a method is invoked on a proxy where the corresponding servant is registered under a different type throws an appropriate exception"
			)]
		public void TestTypeMismatch()
		{
			const ulong servantId = 35;
			var proxy = _client.CreateProxy<IInt32Method>(servantId);
			var subject = new DoesNothing();
			_server.CreateServant(servantId, (IVoidMethod) subject);

			new Action(() => proxy.Do())
				.ShouldThrow<TypeMismatchException>();

			const string reason =
				"Because a method invocation on a mismatched servant-type should not be a reason to tear down the connection";
			_client.IsConnected.Should().BeTrue(reason);
			_server.IsConnected.Should().BeTrue(reason);

			// This line exists to FORCE the GC to NOT collect the subject, which
			// in turn would unregister the servant from the server, thus making the test
			// fail.
			subject.Should().NotBeNull();
		}
	}
}