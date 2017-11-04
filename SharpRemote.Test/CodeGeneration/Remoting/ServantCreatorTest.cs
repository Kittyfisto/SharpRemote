using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration.Remoting;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.Hosting;
using SharpRemote.Tasks;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using SharpRemote.Watchdog;

namespace SharpRemote.Test.CodeGeneration.Remoting
{
	[TestFixture]
	public sealed class ServantCreatorTest
	{
		[SetUp]
		public void SetUp()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Servants");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			ModuleBuilder module = _assembly.DefineDynamicModule(_moduleName);

			var seed = (int) (DateTime.Now.Ticks%Int32.MaxValue);
			Console.WriteLine("Seed: {0}", seed);
			_random = new Random(seed);
			_endPoint = new Mock<IRemotingEndPoint>();
			_channel = new Mock<IEndPointChannel>();
			_creator = new ServantCreator(module);
			_serializer = _creator.Serializer;
		}

		private ServantCreator _creator;
		private Random _random;
		private ulong _objectId;
		private Mock<IEndPointChannel> _channel;
		private Mock<IRemotingEndPoint> _endPoint;
		private AssemblyBuilder _assembly;
		private string _moduleName;
		private ISerializer _serializer;

		[TestFixtureTearDown]
		public void TearDown()
		{
			_assembly.Save(_moduleName);
		}

		private IServant TestGenerate<T>(T subject)
		{
			Type type = null;
			new Action(() => type = _creator.GenerateServant<T>())
				.ShouldNotThrow();

			type.Should().NotBeNull();

			_objectId = (ulong) _random.Next();
			IServant servant = _creator.CreateServant(_endPoint.Object, _channel.Object, _objectId, subject);
			servant.Should().NotBeNull();
			servant.ObjectId.Should().Be(_objectId);
			servant.Subject.Should().BeSameAs(subject);

			return servant;
		}

		[Test]
		public void TestByReferenceParameter()
		{
			IVoidMethodStringParameter actualListener = null;
			var subject = new Mock<IByReferenceParemeterMethodInterface>();
			subject.Setup(x => x.AddListener(It.IsAny<IVoidMethodStringParameter>()))
			       .Callback((IVoidMethodStringParameter l) => actualListener = l);

			var listener = new Mock<IVoidMethodStringParameter>();

			IServant servant = _creator.CreateServant(_endPoint.Object, _channel.Object, 1, subject.Object);

			_endPoint.Setup(x => x.GetExistingOrCreateNewProxy<IVoidMethodStringParameter>(It.IsAny<ulong>()))
			         .Returns((ulong objectId) =>
				         {
					         objectId.Should().Be(12345678912345678912);
					         return listener.Object;
				         });

			var inStream = new MemoryStream();
			var writer = new BinaryWriter(inStream);
			writer.Write(true);
			writer.Write((byte)ByReferenceHint.CreateProxy);
			writer.Write(12345678912345678912);

			inStream.Position = 0;
			var @in = new BinaryReader(inStream);

			var outStream = new MemoryStream();
			servant.Invoke("AddListener", @in, new BinaryWriter(outStream));
			actualListener.Should()
			              .BeSameAs(listener.Object, "because the compiled code should've retrieved the existing proxy by its id");
			outStream.Length.Should().Be(0, "because nothing needed to be written to the outstream");

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}

		[Test]
		public void TestEmpty()
		{
			var subject = new Mock<IEmpty>();
			IServant servant = TestGenerate(subject.Object);
		}

		[Test]
		[Description(
			"Verifies that raising an event on the subject causes the servant to serialize the arguments and send the call via IEndPointChannel.CallRemoteMethod"
			)]
		public void TestEventInt32()
		{
			var subject = new Mock<IEventInt32>();
			IServant servant = TestGenerate(subject.Object);

			bool callRemoteMethodInvoked = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Callback((ulong id, string interfaceName, string methodName, MemoryStream stream) =>
				        {
					        id.Should().Be(servant.ObjectId);
					        methodName.Should().Be("Foobar");
					        stream.Should().NotBeNull();
					        var reader = new BinaryReader(stream);
					        reader.ReadInt32().Should().Be(42);
					        callRemoteMethodInvoked = true;
				        });
			subject.Raise(x => x.Foobar += null, 42);
			callRemoteMethodInvoked.Should().BeTrue();

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}

		[Test]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			IServant servant = TestGenerate(subject.Object);

			subject.Setup(x => x.Value).Returns(Math.PI);

			var outStream = new MemoryStream();
			var @out = new BinaryWriter(outStream);
			servant.Invoke("get_Value", null, @out);

			outStream.Position = 0;
			var reader = new BinaryReader(outStream);
			reader.ReadDouble().Should().BeApproximately(Math.PI, 0);

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}

		[Test]
		[Description("Verifies that a method without the InvokeAttribute is scheduled with the default scheduler")]
		public void TestGetTaskScheduler1()
		{
			var subject = new Mock<IInvokeAttributeMethods>();
			IServant servant = TestGenerate(subject.Object);
			servant.GetTaskScheduler("NoAttribute")
			       .Should().BeNull();
		}

		[Test]
		[Description(
			"Verifies that a method with the InvokeAttribute set to DontSerialize is scheduled with the default scheduler")]
		public void TestGetTaskScheduler2()
		{
			var subject = new Mock<IInvokeAttributeMethods>();
			IServant servant = TestGenerate(subject.Object);
			servant.GetTaskScheduler("DoNotSerialize")
				   .Should().BeNull();
		}

		[Test]
		[Description(
			"Verifies that a method with the InvokeAttribute set to SerializePerType is scheduled with the same scheduler regardless of instance, but different to the default scheduler"
			)]
		public void TestGetTaskScheduler3()
		{
			var subject = new Mock<IInvokeAttributeMethods>();
			IServant servant1 = TestGenerate(subject.Object);
			IServant servant2 = TestGenerate(subject.Object);

			var scheduler = servant1.GetTaskScheduler("SerializePerType");
			scheduler.Should().NotBeNull();
			scheduler.Should().BeOfType<SerialTaskScheduler>();

			scheduler.Should().BeSameAs(servant2.GetTaskScheduler("SerializePerType"));
		}

		[Test]
		[Description(
			"Verifies that a method with the InvokeAttribute set to SerializePerObject is scheduled with the same scheduler, regardless of method, but different to the default scheduler"
			)]
		public void TestGetTaskScheduler4()
		{
			var subject = new Mock<IInvokeAttributeMethods>();
			IServant servant = TestGenerate(subject.Object);

			var scheduler = servant.GetTaskScheduler("SerializePerObject1");
			scheduler.Should().NotBeNull();
			scheduler.Should().BeOfType<SerialTaskScheduler>();

			scheduler.Should().BeSameAs(servant.GetTaskScheduler("SerializePerObject2"));
		}

		[Test]
		[Description(
			"Verifies that a method with the InvokeAttribute set to SerializePerMethod is scheduled with an individual scheduler per method AND object, different to the default and current scheduler"
			)]
		public void TestGetTaskScheduler5()
		{
			var subject = new Mock<IInvokeAttributeMethods>();
			IServant servant1 = TestGenerate(subject.Object);
			IServant servant2 = TestGenerate(subject.Object);

			var scheduler = servant1.GetTaskScheduler("SerializePerMethod1");
			scheduler.Should().NotBeNull();
			scheduler.Should().BeOfType<SerialTaskScheduler>();

			scheduler.Should()
			         .NotBeSameAs(servant1.GetTaskScheduler("SerializePerMethod2"),
			                      "because Dispatch.SerializePerMethod shall behave exactly like Java's synchronized statement");
			scheduler.Should()
			         .NotBeSameAs(servant2.GetTaskScheduler("SerializePerMethod1"),
			                      "because Dispatch.SerializePerMethod shall behave exactly like Java's synchronized statement");
		}

		[Test]
		[Description("Verifies that GetTaskScheduler throws when the given method doesn't exist")]
		public void TestGetTaskScheduler6()
		{
			var subject = new Mock<IInvokeAttributeMethods>();
			IServant servant = TestGenerate(subject.Object);
			new Action(() => servant.GetTaskScheduler("DoesntExist"))
				.ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestIntMethodTypeParameters()
		{
			var subject = new Mock<ISubjectHost>();

			bool disposed = false;
			subject.Setup(x => x.Dispose())
			       .Callback(() => disposed = true);

			ulong? actualId = null;
			Type @interface = null;
			Type @impl = null;
			subject.Setup(x => x.CreateSubject1(It.IsAny<ulong>(), It.IsAny<Type>(), It.IsAny<Type>()))
			       .Callback((ulong id, Type a, Type b) =>
				       {
					       actualId = id;
					       @interface = a;
					       @impl = b;
				       });

			IServant servant = TestGenerate(subject.Object);

			var arguments = new MemoryStream();
			var writer = new BinaryWriter(arguments);
			writer.Write((ulong)42);
			writer.Write(true);
			writer.Write(typeof (IGetStringProperty).AssemblyQualifiedName);
			writer.Write(true);
			writer.Write(typeof (GetStringPropertyImplementation).AssemblyQualifiedName);
			arguments.Position = 0;

			var output = new MemoryStream();
			servant.Invoke("CreateSubject1", new BinaryReader(arguments), new BinaryWriter(output));
			actualId.Should().Be(42ul);
			@interface.Should().Be<IGetStringProperty>();
			@impl.Should().Be<GetStringPropertyImplementation>();

			servant.Invoke("Dispose", null, new BinaryWriter(new MemoryStream()));
			disposed.Should().BeTrue();

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}

		[Test]
		public void TestIntTaskReturnValue()
		{
			var subject = new Mock<IReturnsIntTask>();
			Task<int> innerTask = null;
			subject.Setup(x => x.DoStuff()).Returns(() =>
				{
					innerTask = new Task<int>(() => 42);
					innerTask.Start();
					return innerTask;
				});
			IServant servant = _creator.CreateServant(_endPoint.Object, _channel.Object, 1, subject.Object);
			var output = new MemoryStream();
			servant.Invoke("DoStuff", null, new BinaryWriter(output));
			innerTask.Should().NotBeNull();
			innerTask.Status.Should().Be(TaskStatus.RanToCompletion);
			output.Position = 0;
			var reader = new BinaryReader(output);
			reader.ReadInt32().Should().Be(42);

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}

		[Test]
		public void TestTaskReturnValue()
		{
			var subject = new Mock<IReturnsTask>();
			Task innerTask = null;
			subject.Setup(x => x.DoStuff()).Returns(() =>
				{
					innerTask = new Task(() => { });
					innerTask.Start();
					return innerTask;
				});
			IServant servant = _creator.CreateServant(_endPoint.Object, _channel.Object, 1, subject.Object);
			servant.Invoke("DoStuff", null, new BinaryWriter(new MemoryStream()));
			innerTask.Should().NotBeNull();
			innerTask.Status.Should().Be(TaskStatus.RanToCompletion);

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}

		[Test]
		public void TestVoidMethodTypeParameter()
		{
			var subject = new Mock<IVoidMethodTypeParameter>();
			Type actualType = null;
			subject.Setup(x => x.Do(It.IsAny<Type>())).Callback((Type t) => { actualType = t; });

			IServant servant = TestGenerate(subject.Object);

			var arguments = new MemoryStream();
			var writer = new BinaryWriter(arguments);
			writer.Write(true);
			writer.Write(typeof (string).AssemblyQualifiedName);
			arguments.Position = 0;

			servant.Invoke("Do", new BinaryReader(arguments), new BinaryWriter(new MemoryStream()));
			actualType.Should().Be<string>();

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}

		[Test]
		[Ignore("Investigate why the streams differ in length depending on configuration")]
		public void TestWatchdog()
		{
			var subject = new Mock<IReturnComplexType>();
			var app = new InstalledApplication(new ApplicationDescriptor {Name = "SharpRemote/0.1"});
			app.Files.Add(new InstalledFile
				{
					Id = 1,
					Filename = "SharpRemote.dll",
					FileLength = 212345,
					Folder = Environment.SpecialFolder.CommonProgramFiles
				});
			app.Files.Add(new InstalledFile
				{
					Id = 2,
					Filename = "SharpRemote.Host.exe",
					FileLength = 1234,
					Folder = Environment.SpecialFolder.CommonProgramFiles
				});
			subject.Setup(x => x.CommitInstallation(It.IsAny<long>()))
			       .Returns((long id) => app);
			IServant servant = TestGenerate(subject.Object);

			var arguments = new MemoryStream();
			var writer = new BinaryWriter(arguments);
			writer.Write((long) 42);
			arguments.Position = 0;

			var output = new MemoryStream();
			servant.Invoke("CommitInstallation", new BinaryReader(arguments), new BinaryWriter(output));
			output.Position = 0;
			output.Length.Should().BeInRange(342, 344);

			// Servants hold a weak reference to their subjects, so in order for this test to run 100% of the time,
			// we need to keep the subject alive.
			GC.KeepAlive(subject.Object);
		}
	}
}