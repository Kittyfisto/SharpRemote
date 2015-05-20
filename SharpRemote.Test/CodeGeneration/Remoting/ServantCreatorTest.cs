using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Hosting;
using SharpRemote.Test.Hosting;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.CodeGeneration.Remoting
{
	[TestFixture]
	public sealed class ServantCreatorTest
	{
		private ServantCreator _creator;
		private Random _random;
		private ulong _objectId;
		private Mock<IEndPointChannel> _channel;
		private Mock<IRemotingEndPoint> _endPoint;
		private AssemblyBuilder _assembly;
		private string _moduleName;

		[SetUp]
		public void SetUp()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Servants");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			var module = _assembly.DefineDynamicModule(_moduleName);

			var seed = (int)(DateTime.Now.Ticks % Int32.MaxValue);
			Console.WriteLine("Seed: {0}", seed);
			_random = new Random(seed);
			_endPoint = new Mock<IRemotingEndPoint>();
			_channel = new Mock<IEndPointChannel>();
			_creator = new ServantCreator(module, _endPoint.Object, _channel.Object);
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			_assembly.Save(_moduleName);
		}

		private IServant TestGenerate<T>(T subject)
		{
			Type type = null;
			new Action(() => type = _creator.GenerateSubject<T>())
				.ShouldNotThrow();

			type.Should().NotBeNull();

			_objectId = (ulong)_random.Next();
			var servant = _creator.CreateServant(_objectId, subject);
			servant.Should().NotBeNull();
			servant.ObjectId.Should().Be(_objectId);
			servant.Subject.Should().BeSameAs(subject);

			return servant;
		}

		[Test]
		[Description("Verifies that raising an event on the subject causes the servant to serialize the arguments and send the call via IEndPointChannel.CallRemoteMethod")]
		public void TestEventInt32()
		{
			var subject = new Mock<IEventInt32>();
			var servant = TestGenerate(subject.Object);

			var callRemoteMethodInvoked = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Callback((ulong id, string methodName, MemoryStream stream) =>
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
		public void TestEmpty()
		{
			var subject = new Mock<IEmpty>();
			var servant = TestGenerate(subject.Object);
		}

		[Test]
		public void TestVoidMethodTypeParameter()
		{
			var subject = new Mock<IVoidMethodTypeParameter>();
			Type actualType = null;
			subject.Setup(x => x.Do(It.IsAny<Type>())).Callback((Type t) =>
				{
					actualType = t;
				});

			var servant = TestGenerate(subject.Object);

			var arguments = new MemoryStream();
			var writer = new BinaryWriter(arguments);
			writer.Write(true);
			writer.Write(typeof(string).AssemblyQualifiedName);
			arguments.Position = 0;

			servant.InvokeMethod("Do", new BinaryReader(arguments), new BinaryWriter(new MemoryStream()));
			actualType.Should().Be<string>();
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

			var servant = TestGenerate(subject.Object);

			var arguments = new MemoryStream();
			var writer = new BinaryWriter(arguments);
			writer.Write(true);
			writer.Write(typeof(IGetStringProperty).AssemblyQualifiedName);
			writer.Write(true);
			writer.Write(typeof(GetStringPropertyImplementation).AssemblyQualifiedName);
			arguments.Position = 0;

			var output = new MemoryStream();
			servant.InvokeMethod("CreateSubject1", new BinaryReader(arguments), new BinaryWriter(output));
			@interface.Should().Be<IGetStringProperty>();
			@impl.Should().Be<GetStringPropertyImplementation>();

			servant.InvokeMethod("Dispose", null, new BinaryWriter(new MemoryStream()));
			disposed.Should().BeTrue();
		}

		[Test]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			var servant = TestGenerate(subject.Object);

			subject.Setup(x => x.Value).Returns(Math.PI);

			var outStream = new MemoryStream();
			var @out = new BinaryWriter(outStream);
			servant.InvokeMethod("get_Value", null, @out);

			outStream.Position = 0;
			var reader = new BinaryReader(outStream);
			reader.ReadDouble().Should().BeApproximately(Math.PI, 0);
		}

		[Test]
		public void TestByReferenceParameter()
		{
			IVoidMethodStringParameter actualListener = null;
			var subject = new Mock<IByReferenceParemeterMethodInterface>();
			subject.Setup(x => x.AddListener(It.IsAny<IVoidMethodStringParameter>()))
				   .Callback((IVoidMethodStringParameter l) => actualListener = l);

			var listener = new Mock<IVoidMethodStringParameter>();

			var servant = _creator.CreateServant(1, subject.Object);

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
			servant.InvokeMethod("AddListener", @in, new BinaryWriter(outStream));
			actualListener.Should().BeSameAs(listener.Object, "because the compiled code should've retrieved the existing proxy by its id");
			outStream.Length.Should().Be(0, "because nothing needed to be written to the outstream");
		}
	}
}