using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration.Remoting;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.Tasks;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Remoting
{
	[TestFixture]
	public sealed class RemotingProxyCreatorTest
	{
		private RemotingProxyCreator _creator;
		private Mock<IEndPointChannel> _channel;
		private Random _random;
		private ulong _objectId;
		private AssemblyBuilder _assembly;
		private string _moduleName;
		private Mock<IRemotingEndPoint> _endPoint;

		[OneTimeSetUp]
		public void SetUp()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Proxies");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			ModuleBuilder module = _assembly.DefineDynamicModule(_moduleName);

			var seed = (int) (DateTime.Now.Ticks%Int32.MaxValue);
			Console.WriteLine("Seed: {0}", seed);
			_random = new Random(seed);
			_endPoint = new Mock<IRemotingEndPoint>();

			_channel = new Mock<IEndPointChannel>();
			_creator = new RemotingProxyCreator(module);
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			_assembly.Save(_moduleName);
		}

		private T TestGenerate<T>()
		{
			Type type = null;
			new Action(() => type = _creator.GenerateProxy<T>())
				.ShouldNotThrow();

			type.Should().NotBeNull();

			_objectId = (ulong) _random.Next();
			var obj = _creator.CreateProxy<T>(_endPoint.Object, _channel.Object, _objectId);
			var proxy = obj as IProxy;
			proxy.Should().NotBeNull();
			proxy.ObjectId.Should().Be(_objectId);

			return obj;
		}

		[Test]
		[Description("Verifies that [ByReference] objects are marshalled by embedding the [ByReference] interface and the grain-object-id into the stream")]
		public void TestByReferenceParameter1()
		{
			_endPoint.Setup(
				x => x.GetExistingOrCreateNewServant(It.IsAny<IVoidMethodStringParameter>()))
			         .Returns((IVoidMethodStringParameter subject) =>
				         {
					         var ret = new Mock<IServant>();
					         ret.Setup(x => x.ObjectId).Returns(12345678912345678912);
					         ret.Setup(x => x.Subject).Returns(subject);
					         return ret.Object;
				         });

			var proxy = TestGenerate<IByReferenceParemeterMethodInterface>();
			var listener = new Mock<IVoidMethodStringParameter>();

			bool addListenerCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        interfaceName.Should().Be("SharpRemote.Test.Types.Interfaces.IByReferenceParemeterMethodInterface");
					        methodName.Should().Be("AddListener");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(10);
					        var reader = new BinaryReader(stream);
					        reader.ReadBoolean().Should().BeTrue("Because a non-null by-reference object has been serialized");
					        reader.ReadByte().Should().Be((byte)ByReferenceHint.CreateProxy);
					        reader.ReadUInt64().Should().Be(12345678912345678912);

					        addListenerCalled = true;
					        return null;
				        });

			proxy.AddListener(listener.Object);
			addListenerCalled.Should().BeTrue();
		}

		[Test]
		[Description("Verifies that when a [ByReference] is marshalled, when it's not known at compile time, then its ByReference interface is embedded in the stream")]
		public void TestByReferenceParameter2()
		{
			_endPoint.Setup(
				x => x.GetExistingOrCreateNewServant(It.IsAny<IVoidMethodStringParameter>()))
					 .Returns((IVoidMethodStringParameter subject) =>
					 {
						 var ret = new Mock<IServant>();
						 ret.Setup(x => x.ObjectId).Returns(12345678912345678912);
						 ret.Setup(x => x.Subject).Returns(subject);
						 return ret.Object;
					 });

			var proxy = TestGenerate<IVoidMethodObjectParameter>();
			var listener = new Mock<IVoidMethodStringParameter>();

			bool addListenerCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						interfaceName.Should().Be("SharpRemote.Test.Types.Interfaces.IVoidMethodObjectParameter");
						methodName.Should().Be("AddListener");
						stream.Should().NotBeNull();
						var reader = new BinaryReader(stream);
						reader.ReadString()
						      .Should()
						      .Be(typeof (IVoidMethodStringParameter).AssemblyQualifiedName,
						          "Because the object's by reference interface type should be embedded into the stream");
						reader.ReadByte().Should().Be((byte) ByReferenceHint.CreateProxy);
						reader.ReadUInt64().Should().Be(12345678912345678912);

						addListenerCalled = true;
						return null;
					});

			proxy.AddListener(listener.Object);
			addListenerCalled.Should().BeTrue();
		}

		[Test]
		public void TestEmpty()
		{
			TestGenerate<IEmpty>();
		}

		[Test]
		public void TestEventInt32()
		{
			var proxy = TestGenerate<IEventInt32>();
			int actualValue = 0;
			Action<int> tmp = value => { actualValue = value; };
			proxy.Foobar += tmp;

			var input = new MemoryStream();
			var writer = new BinaryWriter(input);
			writer.Write(42);
			writer.Flush();
			input.Position = 0;
			var reader = new BinaryReader(input);
			var output = new MemoryStream();
			writer = new BinaryWriter(output);
			((IProxy) proxy).Invoke("Foobar", reader, writer);

			actualValue.Should().Be(42);
		}

		[Test]
		public void TestGetDoubleProperty()
		{
			var proxy = TestGenerate<IGetDoubleProperty>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        interfaceName.Should().Be("SharpRemote.Test.Types.Interfaces.PrimitiveTypes.IGetDoubleProperty");
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write(Math.PI);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(Math.PI);
		}

		[Test]
		public void TestGetFloatProperty()
		{
			var proxy = TestGenerate<IGetFloatProperty>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        interfaceName.Should().Be("SharpRemote.Test.Types.Interfaces.PrimitiveTypes.IGetFloatProperty");
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write((float) Math.PI);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be((float) Math.PI);
		}

		[Test]
		public void TestGetStringProperty()
		{
			var proxy = TestGenerate<IGetStringProperty>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        interfaceName.Should().Be("SharpRemote.Test.Types.Interfaces.PrimitiveTypes.IGetStringProperty");
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write(true);
					        outWriter.Write("Lorem Ipsum");
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be("Lorem Ipsum");
		}

		[Test]
		[Description("Verifies that a method without the InvokeAttribute is scheduled with the default scheduler")]
		public void TestGetTaskScheduler1()
		{
			var servant = (IGrain) TestGenerate<IInvokeAttributeEvents>();
			servant.GetTaskScheduler("NoAttribute")
			       .Should().BeNull();
		}

		[Test]
		[Description(
			"Verifies that a method with the InvokeAttribute set to DontSerialize is scheduled with the default scheduler")]
		public void TestGetTaskScheduler2()
		{
			var servant = (IGrain) TestGenerate<IInvokeAttributeEvents>();
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
			var servant1 = (IGrain) TestGenerate<IInvokeAttributeEvents>();
			var servant2 = (IGrain) TestGenerate<IInvokeAttributeEvents>();

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
			var servant = (IGrain) TestGenerate<IInvokeAttributeEvents>();

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
			var servant1 = (IGrain) TestGenerate<IInvokeAttributeEvents>();
			var servant2 = (IGrain) TestGenerate<IInvokeAttributeEvents>();

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
			var servant = (IGrain) TestGenerate<IInvokeAttributeEvents>();
			new Action(() => servant.GetTaskScheduler("DoesntExist"))
				.ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestGetUÍnt16Property()
		{
			var proxy = TestGenerate<IGetUInt16Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write((ushort) 56866);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(56866);
		}

		[Test]
		public void TestGetUÍnt32Property()
		{
			var proxy = TestGenerate<IGetUInt32Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write(2341212312);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(2341212312);
		}

		[Test]
		public void TestGetUÍnt64Property()
		{
			var proxy = TestGenerate<IGetUInt64Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        interfaceName.Should().Be("SharpRemote.Test.Types.Interfaces.PrimitiveTypes.IGetUInt64Property");
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write((ulong) 53512341212312);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(53512341212312);
		}

		[Test]
		public void TestGetUÍnt8Property()
		{
			var proxy = TestGenerate<IGetUInt8Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write((byte) 255);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(255);
		}

		[Test]
		public void TestGetÍnt16Property()
		{
			var proxy = TestGenerate<IGetInt16Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write((short) -31098);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(-31098);
		}

		[Test]
		public void TestGetÍnt32Property()
		{
			var proxy = TestGenerate<IGetInt32Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write(-2141212312);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(-2141212312);
		}

		[Test]
		public void TestGetÍnt64Property()
		{
			var proxy = TestGenerate<IGetInt64Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        interfaceName.Should().Be("SharpRemote.Test.Types.Interfaces.PrimitiveTypes.IGetInt64Property");
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write(-53512341212312);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(-53512341212312);
		}

		[Test]
		public void TestGetÍnt8Property()
		{
			var proxy = TestGenerate<IGetInt8Property>();
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("get_Value");
					        stream.Should().BeNull();

					        var outStream = new MemoryStream();
					        var outWriter = new BinaryWriter(outStream);
					        outWriter.Write((sbyte) 127);
					        outStream.Position = 0;
					        return outStream;
				        });

			proxy.Value.Should().Be(127);
		}

		[Test]
		public void TestNonInterfaceClass()
		{
			new Action(() => _creator.GenerateProxy<string>())
				.ShouldThrow<ArgumentException>()
				.WithMessage("Proxies can only be created for interfaces: System.String is not an interface");
		}

		[Test]
		public void TestNonInterfaceStruct()
		{
			new Action(() => _creator.GenerateProxy<long>())
				.ShouldThrow<ArgumentException>()
				.WithMessage("Proxies can only be created for interfaces: System.Int64 is not an interface");
		}

		[Test]
		[Description("Verifies that an object where it's known at compile time that it is a by reference type is marshalled correctly without embedding type information")]
		public void TestReturnByReference1()
		{
			var proxy = TestGenerate<IByReferenceReturnMethodInterface>();

			IVoidMethodStringParameter listener = new VoidMethodStringParameter();

			const ulong id = 42;
			bool addListenerCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("AddListener");
					        stream.Should().BeNull();

					        addListenerCalled = true;

					        var output = new MemoryStream();
					        var writer = new BinaryWriter(output);
							writer.Write(true);
							writer.Write((byte)ByReferenceHint.CreateProxy);
					        writer.Write(id);
					        writer.Flush();
					        output.Position = 0;

					        return output;
				        });

			_endPoint.Setup(x => x.GetExistingOrCreateNewProxy<IVoidMethodStringParameter>(It.Is((ulong y) => y == id)))
			         .Returns(listener);

			IVoidMethodStringParameter actualListener = proxy.AddListener();
			addListenerCalled.Should().BeTrue();
			actualListener.Should().NotBeNull();
			actualListener.Should().BeSameAs(listener);
		}

		[Test]
		[Description("Verifies that an object where it's known at compile time that it is a by reference type is marshalled correctly without embedding type information")]
		public void TestReturnByReference2()
		{
			var proxy = TestGenerate<IReturnsObjectMethod>();

			IVoidMethodStringParameter listener = new VoidMethodStringParameter();

			const ulong id = 42;
			bool addListenerCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("GetListener");
						stream.Should().BeNull();

						addListenerCalled = true;

						var output = new MemoryStream();
						var writer = new BinaryWriter(output);
						writer.Write(typeof(IVoidMethodStringParameter).AssemblyQualifiedName);
						writer.Write((byte)ByReferenceHint.CreateProxy);
						writer.Write(id);
						writer.Flush();
						output.Position = 0;

						return output;
					});

			_endPoint.Setup(x => x.GetExistingOrCreateNewProxy<IVoidMethodStringParameter>(It.Is((ulong y) => y == id)))
					 .Returns(listener);

			var actualListener = proxy.GetListener() as IVoidMethodStringParameter;
			addListenerCalled.Should().BeTrue();
			actualListener.Should().NotBeNull();
			actualListener.Should().BeSameAs(listener);
		}

		[Test]
		public void TestTaskReturnIntValue()
		{
			var proxy = TestGenerate<IReturnsIntTask>();

			Thread callingThread = Thread.CurrentThread;
			Thread invokingThread = null;
			_channel.Setup(
				x => x.CallRemoteMethodAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        var t = new Task<MemoryStream>(() =>
						        {
							        invokingThread = Thread.CurrentThread;
							        var output = new MemoryStream();
							        var writer = new BinaryWriter(output);
							        writer.Write(42);
							        writer.Flush();
							        output.Position = 0;
							        return output;
						        });
					        t.Start();
					        return t;
				        });

			Task<int> task = proxy.DoStuff();
			task.Should().NotBeNull();
			task.Result.Should().Be(42);

			invokingThread.Should().NotBeNull();
			callingThread.Should()
			             .NotBe(invokingThread,
			                    "because a method with a Task x() signature should invoke CallRemoteMethod on another thread");
		}

		[Test]
		public void TestTaskReturnValue()
		{
			var proxy = TestGenerate<IReturnsTask>();

			Thread callingThread = Thread.CurrentThread;
			Thread invokingThread = null;
			_channel.Setup(
				x => x.CallRemoteMethodAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        var t = new Task<MemoryStream>(() =>
						        {
									invokingThread = Thread.CurrentThread;
							        return null;
						        });
							t.Start();
					        return t;
				        });

			Task task = proxy.DoStuff();
			task.Should().NotBeNull();
			task.Wait();

			invokingThread.Should().NotBeNull();
			callingThread.Should()
			             .NotBe(invokingThread,
			                    "because a method with a Task x() signature should invoke CallRemoteMethod on another thread");
		}

		[Test]
		public void TestTaskThrowException()
		{
			var proxy = TestGenerate<IReturnsTask>();

			Thread callingThread = Thread.CurrentThread;
			Thread invokingThread = null;
			_channel.Setup(
				x => x.CallRemoteMethodAsync(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
					{
						var t = new Task<MemoryStream>(() =>
							{
								invokingThread = Thread.CurrentThread;
							throw new SystemException("What would you do for a klondyke bar?");
						});
						t.Start();
						return t;
					});

			Task task = proxy.DoStuff();
			task.Should().NotBeNull();
			new Action(task.Wait)
				.ShouldThrow<SystemException>()
				.WithMessage("What would you do for a klondyke bar?");
			task.IsFaulted.Should().BeTrue();

			invokingThread.Should().NotBeNull();
			callingThread.Should()
						 .NotBe(invokingThread,
								"because a method with a Task x() signature should invoke CallRemoteMethod on another thread");
		}

		[Test]
		public void TestVoidMethodBaseClassParameter1()
		{
			var proxy = TestGenerate<IVoidMethodBaseClassParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        var reader = new BinaryReader(stream);
					        reader.ReadString().Should().Be(typeof (Birke).AssemblyQualifiedName);

					        reader.ReadBoolean().Should().BeTrue();
					        reader.ReadString().Should().Be("Foobar");
					        reader.ReadByte().Should().Be(42);
					        reader.ReadDouble().Should().Be(Math.PI);
					        stream.Position.Should().Be(stream.Length);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(new Birke
				{
					A = Math.PI,
					B = 42,
					C = "Foobar",
				});
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodBaseClassParameter2()
		{
			var proxy = TestGenerate<IVoidMethodBaseClassParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(5);
					        var reader = new BinaryReader(stream);
					        string value1 = reader.ReadString();
					        value1.Should().Be("null");

					        doCalled = true;
					        return null;
				        });

			proxy.Do(null);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodDoubleParameter()
		{
			var proxy = TestGenerate<IVoidMethodDoubleParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(8);
					        var reader = new BinaryReader(stream);
					        double value = reader.ReadDouble();
					        value.Should().BeApproximately(Math.PI, 0);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(Math.PI);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodFloatParameter()
		{
			var proxy = TestGenerate<IVoidMethodFloatParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(4);
					        var reader = new BinaryReader(stream);
					        float value = reader.ReadSingle();
					        value.Should().BeApproximately((float) Math.PI, 0);

					        doCalled = true;
					        return null;
				        });

			proxy.Do((float) Math.PI);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt16Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt16Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(2);
					        var reader = new BinaryReader(stream);
					        short value = reader.ReadInt16();
					        value.Should().Be(-32098);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(-32098);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt32Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt32Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(4);
					        var reader = new BinaryReader(stream);
					        int value = reader.ReadInt32();
					        value.Should().Be(-32);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(-32);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt64Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt64Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(8);
					        var reader = new BinaryReader(stream);
					        long value = reader.ReadInt64();
					        value.Should().Be(-64);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(-64);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt8Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt8Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(1);
					        var reader = new BinaryReader(stream);
					        sbyte value = reader.ReadSByte();
					        value.Should().Be(-8);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(-8);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodNoParameters()
		{
			var proxy = TestGenerate<IVoidMethodNoParameters>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().BeNull();

					        doCalled = true;
					        return null;
				        });

			proxy.Do();
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodSealedClassParameter1()
		{
			var proxy = TestGenerate<IVoidMethodSealedClassParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(21);
					        var reader = new BinaryReader(stream);
					        bool value0 = reader.ReadBoolean();
					        value0.Should().BeTrue();
					        reader.ReadDouble().Should().BeApproximately(Math.PI, 0);
					        reader.ReadInt32().Should().Be(42);
					        reader.ReadBoolean().Should().BeTrue();
					        reader.ReadString().Should().Be("Foobar");

					        doCalled = true;
					        return null;
				        });

			proxy.Do(new FieldSealedClass
				{
					A = Math.PI,
					B = 42,
					C = "Foobar",
				});
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodSealedClassParameter2()
		{
			var proxy = TestGenerate<IVoidMethodSealedClassParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(1);
					        var reader = new BinaryReader(stream);
					        bool value0 = reader.ReadBoolean();
					        value0.Should().BeFalse();

					        doCalled = true;
					        return null;
				        });

			proxy.Do(null);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodStringParameter()
		{
			var proxy = TestGenerate<IVoidMethodStringParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        var reader = new BinaryReader(stream);
					        reader.ReadBoolean().Should().BeTrue();
					        reader.ReadString().Should().Be("Foobar");

					        doCalled = true;
					        return null;
				        });

			proxy.Do("Foobar");
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodStructParameter()
		{
			var proxy = TestGenerate<IVoidMethodStructParameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(20);
					        var reader = new BinaryReader(stream);
					        reader.ReadDouble().Should().BeApproximately(Math.PI, 0);
					        reader.ReadInt32().Should().Be(42);
					        reader.ReadBoolean().Should().BeTrue();
					        reader.ReadString().Should().Be("Foobar");

					        doCalled = true;
					        return null;
				        });

			proxy.Do(new FieldStruct
				{
					A = Math.PI,
					B = 42,
					C = "Foobar",
				});
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt16Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt16Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(2);
					        var reader = new BinaryReader(stream);
					        ushort value = reader.ReadUInt16();
					        value.Should().Be(59876);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(59876);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt32Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt32Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(4);
					        var reader = new BinaryReader(stream);
					        uint value = reader.ReadUInt32();
					        value.Should().Be(3121002121);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(3121002121);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt64Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt64Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(8);
					        var reader = new BinaryReader(stream);
					        long value = reader.ReadInt64();
					        value.Should().Be(42123312312321);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(42123312312321);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt8Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt8Parameter>();

			bool doCalled = false;
			_channel.Setup(
				x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string interfaceName, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
					        methodName.Should().Be("Do");
					        stream.Should().NotBeNull();
					        stream.Length.Should().Be(1);
					        var reader = new BinaryReader(stream);
					        byte value = reader.ReadByte();
					        value.Should().Be(254);

					        doCalled = true;
					        return null;
				        });

			proxy.Do(254);
			doCalled.Should().BeTrue();
		}
		
		[Test]
		[LocalTest("")]
		[Repeat(500)]
		public void TestConcurrency()
		{
			var tasks = Enumerable.Range(0, 8).Select(unused => Task.Factory.StartNew(() =>
			{
				_creator.CreateProxy<IActionEventStringArray>(_endPoint.Object, _channel.Object, 1);
			}, TaskCreationOptions.LongRunning)).ToArray();
			new Action(() => Task.WaitAll(tasks))
				.ShouldNotThrow();
		}
	}
}