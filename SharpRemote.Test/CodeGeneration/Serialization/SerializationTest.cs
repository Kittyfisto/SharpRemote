using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed partial class SerializationTest
	{
		private Serializer _serializer;
		private AssemblyBuilder _assembly;
		private string _moduleName;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			ModuleBuilder module = _assembly.DefineDynamicModule(_moduleName);
			_serializer = new Serializer(module);
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
		public void TestSingleton()
		{
			_serializer.RegisterSingleton<Singleton>(typeof (Singleton).GetProperty("Instance").GetMethod);
			_serializer.ShouldRoundtrip(Singleton.Instance);
			_serializer.ShouldRoundtripEnumeration(new[]
				{
					null,
					Singleton.Instance
				});
			_serializer.ShouldRoundtrip(new ClassWithSingleton());
			_serializer.ShouldRoundtrip(new ClassWithSingleton
				{
					That = Singleton.Instance
				});
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
	}
}