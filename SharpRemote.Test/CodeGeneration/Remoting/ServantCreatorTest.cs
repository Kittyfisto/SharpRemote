using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Hosting;
using SharpRemote.Test.Hosting;
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
			_creator = new ServantCreator(module, _endPoint.Object, _channel.Object);
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
			IServant servant = _creator.CreateServant(_objectId, subject);
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

			IServant servant = _creator.CreateServant(1, subject.Object);

			_endPoint.Setup(x => x.GetExistingOrCreateNewProxy<IVoidMethodStringParameter>(It.IsAny<ulong>()))
			         .Returns((ulong objectId) =>
				         {
					         objectId.Should().Be(12345678912345678912);
					         return listener.Object;
				         });

			var inStream = new MemoryStream();
			var writer = new BinaryWriter(inStream);
			writer.Write(12345678912345678912);

			inStream.Position = 0;
			var @in = new BinaryReader(inStream);

			var outStream = new MemoryStream();
			servant.Invoke("AddListener", @in, new BinaryWriter(outStream));
			actualListener.Should()
			              .BeSameAs(listener.Object, "because the compiled code should've retrieved the existing proxy by its id");
			outStream.Length.Should().Be(0, "because nothing needed to be written to the outstream");
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
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
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
		}

		[Test]
		public void TestIntMethodTypeParameters()
		{
			var subject = new Mock<ISubjectHost>();

			bool disposed = false;
			subject.Setup(x => x.Dispose())
			       .Callback(() => disposed = true);

			Type @interface = null;
			Type @impl = null;
			subject.Setup(x => x.CreateSubject1(It.IsAny<Type>(), It.IsAny<Type>()))
			       .Returns((Type a, Type b) =>
				       {
					       @interface = a;
					       @impl = b;
					       return 42;
				       });

			IServant servant = TestGenerate(subject.Object);

			var arguments = new MemoryStream();
			var writer = new BinaryWriter(arguments);
			writer.Write(true);
			writer.Write(typeof (IGetStringProperty).AssemblyQualifiedName);
			writer.Write(true);
			writer.Write(typeof (GetStringPropertyImplementation).AssemblyQualifiedName);
			arguments.Position = 0;

			var output = new MemoryStream();
			servant.Invoke("CreateSubject1", new BinaryReader(arguments), new BinaryWriter(output));
			@interface.Should().Be<IGetStringProperty>();
			@impl.Should().Be<GetStringPropertyImplementation>();

			servant.Invoke("Dispose", null, new BinaryWriter(new MemoryStream()));
			disposed.Should().BeTrue();
		}

		[Test]
		public void TestTaskReturnValue()
		{
			var subject = new Mock<IReturnsTask>();
			Task innerTask = null;
			subject.Setup(x => x.DoStuff()).Returns(() =>
				{
					innerTask = new Task(() =>
						{
						});
					innerTask.Start();
					return innerTask;
				});
			IServant servant = _creator.CreateServant(1, subject.Object);
			servant.Invoke("DoStuff", null, new BinaryWriter(new MemoryStream()));
			innerTask.Should().NotBeNull();
			innerTask.Status.Should().Be(TaskStatus.RanToCompletion);
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
			IServant servant = _creator.CreateServant(1, subject.Object);
			var output = new MemoryStream();
			servant.Invoke("DoStuff", null, new BinaryWriter(output));
			innerTask.Should().NotBeNull();
			innerTask.Status.Should().Be(TaskStatus.RanToCompletion);
			output.Position = 0;
			var reader = new BinaryReader(output);
			reader.ReadInt32().Should().Be(42);
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
		}

		[Test]
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
			output.Length.Should().BeInRange(330, 333);
		}
	}
}