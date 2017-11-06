using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed partial class SerializationTest
	{
		private BinarySerializer _serializer;
		private AssemblyBuilder _assembly;
		private string _moduleName;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			ModuleBuilder module = _assembly.DefineDynamicModule(_moduleName);
			_serializer = new BinarySerializer(module);
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			_assembly.Save(_moduleName);
		}

		[Test]
		public void TestBinaryTree()
		{
			var tree = new BinaryTreeNode
				{
					Value = 0,
					Left = new BinaryTreeNode
						{
							Value = -1
						},
					Right = new BinaryTreeNode
						{
							Value = 1,
							Left = new BinaryTreeNode
								{
									Value = 0.5
								}
						}
				};
			_serializer.ShouldRoundtrip(tree);
		}

		[Test]
		public void TestBool()
		{
			_serializer.RegisterType<bool>();
			_serializer.ShouldRoundtrip(true);
			_serializer.ShouldRoundtrip(false);
		}

		[Test]
		public void TestClassWithTypeHashSet()
		{
			_serializer.RegisterType<ClassWithTypeHashSet>();
			var value = new ClassWithTypeHashSet
				{
					Values = new HashSet<Type>
						{
							typeof (int),
							typeof (ClassWithTypeHashSet),
							typeof (Type)
						}
				};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestFieldSealedClass()
		{
			_serializer.RegisterType<FieldSealedClass>();
			var value = new FieldSealedClass
				{
					A = 1321331.21312,
					B = 322132312,
					C = "Rise, lord Vader!"
				};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestFieldStruct()
		{
			_serializer.RegisterType<FieldStruct>();
			var value = new FieldStruct
				{
					A = Math.PI,
					B = 42,
					C = "Foobar"
				};
			_serializer.ShouldRoundtrip(value);

			value = new FieldStruct
				{
					A = double.MinValue,
					B = int.MaxValue,
					C = null
				};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestInt16()
		{
			_serializer.RegisterType<Int16>();
			_serializer.ShouldRoundtrip((Int16) (-31187));
		}

		[Test]
		public void TestInt32()
		{
			_serializer.RegisterType<Int32>();
			_serializer.ShouldRoundtrip(42);
		}

		[Test]
		public void TestInt64()
		{
			_serializer.RegisterType<Int64>();
			_serializer.ShouldRoundtrip(-345442343232423);
		}

		[Test]
		public void TestInt8()
		{
			_serializer.RegisterType<sbyte>();
			_serializer.ShouldRoundtrip((sbyte) (-128));
		}

		[Test]
		public void TestNestedFieldStruct()
		{
			var value = new NestedFieldStruct
				{
					N1 = new PropertySealedClass
						{
							Value1 = "Bs",
							Value2 = 80793,
							Value3 = 30987.12234
						},
					N2 = new FieldStruct
						{
							A = -897761.1232,
							B = -3214312,
							C = "Blubba\r\ndawawd\tdddD"
						}
				};

			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestNonSealedClass()
		{
			_serializer.RegisterType<NonSealedClass>();
			_serializer.ShouldRoundtrip(new NonSealedClass());
			_serializer.ShouldRoundtrip(new NonSealedClass
				{
					Value1 = "FOobar",
					Value2 = true
				});
		}

		[Test]
		public void TestNull()
		{
			_serializer.ShouldRoundtrip(null);
		}

		[Test]
		public void TestPropertySealedClass()
		{
			var value = new PropertySealedClass
				{
					Value1 = "Execute Order 66",
					Value2 = -342131231,
					Value3 = Math.PI
				};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestPropertyStruct()
		{
			var value = new PropertyStruct {Value = "Execute Order 66"};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestSingleton1()
		{
			_serializer.ShouldRoundtrip(Singleton.GetInstance());
			_serializer.ShouldRoundtripEnumeration(new[]
				{
					null,
					Singleton.GetInstance()
				});
			_serializer.ShouldRoundtrip(new ClassWithSingleton());
			_serializer.ShouldRoundtrip(new ClassWithSingleton
				{
					That = Singleton.GetInstance()
				});
		}

		[Test]
		public void TestSingleton2()
		{
			var values = _serializer.Roundtrip(new ISingleton[] { Singleton2.Instance});
			values.Should().HaveCount(1);
			values[0].Should().BeSameAs(Singleton2.Instance);
		}

		[Test]
		public void TestString()
		{
			_serializer.RegisterType<string>();
			_serializer.ShouldRoundtrip(null);
			_serializer.ShouldRoundtrip("Foobar");
			_serializer.ShouldRoundtrip(string.Empty);
		}

		[Test]
		public void TestUInt16()
		{
			_serializer.RegisterType<UInt16>();
			_serializer.ShouldRoundtrip((UInt16) 56178);
		}

		[Test]
		public void TestUInt32()
		{
			_serializer.RegisterType<UInt32>();
			_serializer.ShouldRoundtrip(42u);
		}

		[Test]
		public void TestUInt64()
		{
			_serializer.RegisterType<UInt64>();
			_serializer.ShouldRoundtrip(9899045442343232423);
		}

		[Test]
		public void TestUInt8()
		{
			_serializer.RegisterType<byte>();
			_serializer.ShouldRoundtrip((byte) 255);
		}

		[Test]
		public void TestNullableStructProperty()
		{
			_serializer.RegisterType<ClassWithNullableStructProperty>();
			_serializer.ShouldRoundtrip(new ClassWithNullableStructProperty {Value = 42});
			_serializer.ShouldRoundtrip(new ClassWithNullableStructProperty { Value = null });
		}

		[Test]
		[Description("Verifies that serializing a [ByReference] type queries the endpoint for the servant and serializes both the interface type and the grain's object-the id to the stream")]
		public void TestByReferenceSerialize1()
		{
			_serializer.RegisterType<IByReferenceType>();

			var value = new ByReferenceClass();
			const long objectId = 42;
			var endPoint = new Mock<IRemotingEndPoint>();

			endPoint.Setup(x => x.GetExistingOrCreateNewServant(It.IsAny<IByReferenceType>()))
			        .Returns((IByReferenceType x) =>
				        {
					        x.Should().BeSameAs(value);
					        var servant = new Mock<IServant>();
					        servant.Setup(y => y.ObjectId).Returns(objectId);
					        return servant.Object;
				        });

			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			using (var reader = new BinaryReader(stream))
			{
				_serializer.WriteObject(writer, value, endPoint.Object);
				writer.Flush();
				stream.Position = 0;

				reader.ReadString().Should().Be(typeof(IByReferenceType).AssemblyQualifiedName);
				reader.ReadByte().Should().Be((byte)ByReferenceHint.CreateProxy);
				reader.ReadInt64().Should().Be(objectId);

				stream.Position.Should().Be(stream.Length, "because we should've consumed the entire stream");
			}
		}

		[Test]
		[Description("Verifies that serializing a [ByReference] type proxy, then the proxy's object id is serialized to the stream, if the proxy belongs to the serializing endpoint")]
		public void TestByReferenceSerialize2()
		{
			_serializer.RegisterType<IByReferenceType>();

			const long objectId = 42;
			var endPoint = new Mock<IRemotingEndPoint>();

			var value = new Mock<IByReferenceType>();
			var proxy = value.As<IProxy>();
			proxy.Setup(x => x.EndPoint).Returns(endPoint.Object);
			proxy.Setup(x => x.ObjectId).Returns(objectId);

			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			using (var reader = new BinaryReader(stream))
			{
				_serializer.WriteObject(writer, value.Object, endPoint.Object);
				writer.Flush();
				stream.Position = 0;

				reader.ReadString().Should().Be(typeof(IByReferenceType).AssemblyQualifiedName);
				reader.ReadByte().Should().Be((byte)ByReferenceHint.RetrieveSubject);
				reader.ReadInt64().Should().Be(objectId);

				stream.Position.Should().Be(stream.Length, "because we should've consumed the entire stream");
			}
		}

		[Test]
		[Description("Verifies that deserializing a [ByReference] type queries the endpoint for the proxy by the id embedded in the stream")]
		public void TestByReferenceDeserialize()
		{
			var endPoint = new Mock<IRemotingEndPoint>();

			const ulong objectId = 42;
			var value = new ByReferenceClass();
			endPoint.Setup(x => x.GetExistingOrCreateNewProxy<IByReferenceType>(It.IsAny<ulong>()))
					.Returns((ulong id) =>
					{
						id.Should().Be(objectId);
						return value;
					});

			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			using (var reader = new BinaryReader(stream))
			{
				writer.Write(typeof(IByReferenceType).AssemblyQualifiedName);
				writer.Write((byte)ByReferenceHint.CreateProxy);
				writer.Write(objectId);

				writer.Flush();
				stream.Position = 0;

				var actualValue = _serializer.ReadObject(reader, endPoint.Object);
				actualValue.Should().BeSameAs(value);

				stream.Position.Should().Be(stream.Length, "because we should've consumed the entire stream");
			}
		}

		[Test]
		public void TestClassWithNullableTimeSpan()
		{
			var value = new ClassWithNullableTimeSpan
				{
					Value = TimeSpan.FromSeconds(1.5)
				};

			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		[Description("Verifies that a class all four callbacks are invoked on the correct objects in the correct order")]
		public void TestClassWithDeserializationCallback()
		{
			var value = new ClassWithSerializationCallbacks();
			var actualValue = _serializer.Roundtrip(value);

			const string reason = "because those two callbacks should've been invoked in that order";
			value.Callbacks.Should().Equal(new object[]
			{
				"BeforeSerialization",
				"AfterSerialization"
			}, reason);

			actualValue.Callbacks.Should().Equal(new object[]
			{
				"BeforeDeserialization",
				"AfterDeserialization"
			}, reason);
		}

		[Test]
		public void TestClassWithBeforeSerializeCallback()
		{
			var value = new ClassWithBeforeSerializeCallback
			{
				Type = typeof(string)
			};
			var actualValue = _serializer.Roundtrip(value);

			actualValue.Should().NotBeNull();
			const string reason = "because the BeforeSerialize callback should've been invoked which sets this value";
			actualValue.SerializedType.Should().Be(typeof(string).AssemblyQualifiedName, reason);
			actualValue.SomeImortantInfo.Should().Be(typeof(string).MetadataToken, reason);
		}

		[Test]
		public void TestClassWithAfterDeserializeCallback()
		{
			var value = new ClassWithAfterDeserializeCallback
			{
				SerializedType = typeof(double).AssemblyQualifiedName
			};
			var actualValue = _serializer.Roundtrip(value);

			actualValue.Should().NotBeNull();
			const string reason = "because the AfterDeserialize callback should've been invoked which sets this value";
			actualValue.Type.Should().Be<double>(reason);
		}

		public static void WriteValueNotNull(BinaryWriter writer, ClassWithNullableTimeSpan obj, ISerializer serializer)
		{
			var tmp = obj.Value.HasValue;
			writer.Write(tmp);
			if (tmp)
			{
				writer.Write(obj.Value.Value.Ticks);
			}
		}
	}
}