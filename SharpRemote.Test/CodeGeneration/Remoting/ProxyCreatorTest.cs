using System;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.Classes;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Remoting
{
	[TestFixture]
	public sealed class ProxyCreatorTest
	{
		private ProxyCreator _creator;
		private Mock<IEndPointChannel> _channel;
		private Random _random;
		private ulong _objectId;

		[SetUp]
		public void SetUp()
		{
			var seed = (int) (DateTime.Now.Ticks%Int32.MaxValue);
			Console.WriteLine("Seed: {0}", seed);
			_random = new Random(seed);
			_channel = new Mock<IEndPointChannel>();
			_creator = new ProxyCreator(_channel.Object);
		}

		private T TestGenerate<T>()
		{
			Type type = null;
			new Action(() => type = _creator.GenerateProxy<T>())
				.ShouldNotThrow();

			type.Should().NotBeNull();

			_objectId = (ulong) _random.Next();
			var obj = _creator.CreateProxy<T>(_objectId);
			var proxy = obj as IProxy;
			proxy.Should().NotBeNull();
			proxy.ObjectId.Should().Be(_objectId);

			return obj;
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
		public void TestEmpty()
		{
			TestGenerate<IEmpty>();
		}

		[Test]
		public void TestGetStringProperty()
		{
			var proxy = TestGenerate<IGetStringProperty>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
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
		public void TestGetDoubleProperty()
		{
			var proxy = TestGenerate<IGetDoubleProperty>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			        .Returns((ulong objectId, string methodName, Stream stream) =>
				        {
					        objectId.Should().Be(((IProxy) proxy).ObjectId);
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
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("get_Value");
						stream.Should().BeNull();

						var outStream = new MemoryStream();
						var outWriter = new BinaryWriter(outStream);
						outWriter.Write((float)Math.PI);
						outStream.Position = 0;
						return outStream;
					});

			proxy.Value.Should().Be((float)Math.PI);
		}

		[Test]
		public void TestGetÍnt64Property()
		{
			var proxy = TestGenerate<IGetInt64Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
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
		public void TestGetUÍnt64Property()
		{
			var proxy = TestGenerate<IGetUInt64Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("get_Value");
						stream.Should().BeNull();

						var outStream = new MemoryStream();
						var outWriter = new BinaryWriter(outStream);
						outWriter.Write((ulong)53512341212312);
						outStream.Position = 0;
						return outStream;
					});

			proxy.Value.Should().Be(53512341212312);
		}

		[Test]
		public void TestGetUÍnt32Property()
		{
			var proxy = TestGenerate<IGetUInt32Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
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
		public void TestGetÍnt32Property()
		{
			var proxy = TestGenerate<IGetInt32Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
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
		public void TestGetÍnt16Property()
		{
			var proxy = TestGenerate<IGetInt16Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("get_Value");
						stream.Should().BeNull();

						var outStream = new MemoryStream();
						var outWriter = new BinaryWriter(outStream);
						outWriter.Write((short)-31098);
						outStream.Position = 0;
						return outStream;
					});

			proxy.Value.Should().Be(-31098);
		}

		[Test]
		public void TestGetUÍnt16Property()
		{
			var proxy = TestGenerate<IGetUInt16Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("get_Value");
						stream.Should().BeNull();

						var outStream = new MemoryStream();
						var outWriter = new BinaryWriter(outStream);
						outWriter.Write((ushort)56866);
						outStream.Position = 0;
						return outStream;
					});

			proxy.Value.Should().Be(56866);
		}

		[Test]
		public void TestGetUÍnt8Property()
		{
			var proxy = TestGenerate<IGetUInt8Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("get_Value");
						stream.Should().BeNull();

						var outStream = new MemoryStream();
						var outWriter = new BinaryWriter(outStream);
						outWriter.Write((byte)255);
						outStream.Position = 0;
						return outStream;
					});

			proxy.Value.Should().Be(255);
		}

		[Test]
		public void TestGetÍnt8Property()
		{
			var proxy = TestGenerate<IGetUInt8Property>();
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("get_Value");
						stream.Should().BeNull();

						var outStream = new MemoryStream();
						var outWriter = new BinaryWriter(outStream);
						outWriter.Write((sbyte)127);
						outStream.Position = 0;
						return outStream;
					});

			proxy.Value.Should().Be(127);
		}

		[Test]
		public void TestVoidMethodNoParameters()
		{
			var proxy = TestGenerate<IVoidMethodNoParameters>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().BeNull();

						doCalled = true;
						return null;
					});

			proxy.Do();
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodDoubleParameter()
		{
			var proxy = TestGenerate<IVoidMethodDoubleParameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(8);
						var reader = new BinaryReader(stream);
						var value = reader.ReadDouble();
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
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(4);
						var reader = new BinaryReader(stream);
						var value = reader.ReadSingle();
						value.Should().BeApproximately((float)Math.PI, 0);

						doCalled = true;
						return null;
					});

			proxy.Do((float) Math.PI);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt64Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt64Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(8);
						var reader = new BinaryReader(stream);
						var value = reader.ReadInt64();
						value.Should().Be(-64);

						doCalled = true;
						return null;
					});

			proxy.Do(-64);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt64Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt64Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(8);
						var reader = new BinaryReader(stream);
						var value = reader.ReadInt64();
						value.Should().Be(42123312312321);

						doCalled = true;
						return null;
					});

			proxy.Do(42123312312321);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt32Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt32Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(4);
						var reader = new BinaryReader(stream);
						var value = reader.ReadInt32();
						value.Should().Be(-32);

						doCalled = true;
						return null;
					});

			proxy.Do(-32);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt32Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt32Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(4);
						var reader = new BinaryReader(stream);
						var value = reader.ReadUInt32();
						value.Should().Be(3121002121);

						doCalled = true;
						return null;
					});

			proxy.Do(3121002121);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt16Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt16Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(2);
						var reader = new BinaryReader(stream);
						var value = reader.ReadInt16();
						value.Should().Be(-32098);

						doCalled = true;
						return null;
					});

			proxy.Do(-32098);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt16Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt16Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(2);
						var reader = new BinaryReader(stream);
						var value = reader.ReadUInt16();
						value.Should().Be(59876);

						doCalled = true;
						return null;
					});

			proxy.Do(59876);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodInt8Parameter()
		{
			var proxy = TestGenerate<IVoidMethodInt8Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(1);
						var reader = new BinaryReader(stream);
						var value = reader.ReadSByte();
						value.Should().Be(-8);

						doCalled = true;
						return null;
					});

			proxy.Do(-8);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodUInt8Parameter()
		{
			var proxy = TestGenerate<IVoidMethodUInt8Parameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(1);
						var reader = new BinaryReader(stream);
						var value = reader.ReadByte();
						value.Should().Be(254);

						doCalled = true;
						return null;
					});

			proxy.Do(254);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestVoidMethodStringParameter()
		{
			var proxy = TestGenerate<IVoidMethodStringParameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
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
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
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
		public void TestVoidMethodSealedClassParameter1()
		{
			var proxy = TestGenerate<IVoidMethodSealedClassParameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(21);
						var reader = new BinaryReader(stream);
						var value0 = reader.ReadBoolean();
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
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						stream.Length.Should().Be(1);
						var reader = new BinaryReader(stream);
						var value0 = reader.ReadBoolean();
						value0.Should().BeFalse();

						doCalled = true;
						return null;
					});

			proxy.Do(null);
			doCalled.Should().BeTrue();
		}

		[Test]
		public void TestEventInt32()
		{
			var proxy = TestGenerate<IEventInt32>();
			int actualValue = 0;
			Action<int> tmp = value =>
				{
					actualValue = value;
				};
			proxy.Foobar += tmp;

			var input = new MemoryStream();
			var writer = new BinaryWriter(input);
			writer.Write(42);
			writer.Flush();
			input.Position = 0;
			var reader = new BinaryReader(input);
			var output = new MemoryStream();
			writer = new BinaryWriter(output);
			((IProxy)proxy).InvokeEvent("Foobar", reader, writer);

			actualValue.Should().Be(42);
		}

		[Test]
		[Ignore("TBD")]
		public void TestVoidMethodBaseClassParameter1()
		{
			var proxy = TestGenerate<IVoidMethodBaseClassParameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						//stream.Length.Should().Be(19);
						var reader = new BinaryReader(stream);
						var value1 = reader.ReadString();

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
		[Ignore("TBD")]
		public void TestVoidMethodBaseClassParameter2()
		{
			var proxy = TestGenerate<IVoidMethodBaseClassParameter>();

			bool doCalled = false;
			_channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
					.Returns((ulong objectId, string methodName, Stream stream) =>
					{
						objectId.Should().Be(((IProxy)proxy).ObjectId);
						methodName.Should().Be("Do");
						stream.Should().NotBeNull();
						//stream.Length.Should().Be(19);
						var reader = new BinaryReader(stream);
						var value1 = reader.ReadString();
						value1.Should().Be(string.Empty);

						doCalled = true;
						return null;
					});

			proxy.Do(null);
			doCalled.Should().BeTrue();
		}
	}
}