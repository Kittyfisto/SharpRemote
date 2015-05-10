using System;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class SerializationTest
	{
		private Serializer _serializer;
		private AssemblyBuilder _assembly;
		private string _moduleName;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var assemblyName = new AssemblyName("SharpRemote.CodeGeneration.Serializer");
			_assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			_moduleName = assemblyName.Name + ".dll";
			var module = _assembly.DefineDynamicModule(_moduleName);
			_serializer = new Serializer(module);
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			_assembly.Save(_moduleName);
		}

		[Test]
		public void TestRoundtripNull()
		{
			_serializer.RoundtripObject(null).Should().BeNull();
		}

		[Test]
		public void TestRoundtripInt8()
		{
			_serializer.RegisterType<sbyte>();
			_serializer.RoundtripObject((sbyte)(-128)).Should().Be((sbyte)(-128));
		}

		[Test]
		public void TestRoundtripUInt8()
		{
			_serializer.RegisterType<byte>();
			_serializer.RoundtripObject((byte)255).Should().Be((byte)255);
		}

		[Test]
		public void TestRoundtripBool()
		{
			_serializer.RegisterType<bool>();
			_serializer.RoundtripObject(true).Should().Be(true);
			_serializer.RoundtripObject(false).Should().Be(false);
		}

		[Test]
		public void TestRoundtripInt16()
		{
			_serializer.RegisterType<Int16>();
			_serializer.RoundtripObject((Int16)(-31187)).Should().Be((Int16)(-31187));
		}

		[Test]
		public void TestRoundtripUInt16()
		{
			_serializer.RegisterType<UInt16>();
			_serializer.RoundtripObject((UInt16)56178).Should().Be((UInt16)56178);
		}

		[Test]
		public void TestRoundtripInt32()
		{
			_serializer.RegisterType<Int32>();
			_serializer.RoundtripObject(42).Should().Be(42);
		}

		[Test]
		public void TestRoundtripUInt32()
		{
			_serializer.RegisterType<UInt32>();
			_serializer.RoundtripObject(42u).Should().Be(42u);
		}

		[Test]
		public void TestRoundtripInt64()
		{
			_serializer.RegisterType<Int64>();
			_serializer.RoundtripObject(-345442343232423).Should().Be(-345442343232423);
		}

		[Test]
		public void TestRoundtripUInt64()
		{
			_serializer.RegisterType<UInt64>();
			_serializer.RoundtripObject(9899045442343232423).Should().Be(9899045442343232423);
		}

		[Test]
		public void TestRoundtripString()
		{
			_serializer.RegisterType<string>();
			_serializer.RoundtripObject(null).Should().Be(null);
			_serializer.RoundtripObject("Foobar").Should().Be("Foobar");
			_serializer.RoundtripObject(string.Empty).Should().Be(string.Empty);
		}

		[Test]
		public void TestRoundtripIPAddress()
		{
			_serializer.RegisterType<IPAddress>();
			_serializer.RoundtripObject(IPAddress.Parse("192.168.0.87")).Should().Be(IPAddress.Parse("192.168.0.87"));
			_serializer.RoundtripObject(IPAddress.IPv6Loopback).Should().Be(IPAddress.IPv6Loopback);
		}

		[Test]
		public void TestRoundtripType()
		{
			_serializer.RegisterType<Type>();
			_serializer.RoundtripObject(typeof (int));
		}

		[Test]
		public void TestRoundtripFieldStruct()
		{
			_serializer.RegisterType<FieldStruct>();
			var value = new FieldStruct
				{
					A = Math.PI,
					B = 42,
					C = "Foobar"
				};
			_serializer.RoundtripObject(value).Should().Be(value);

			value = new FieldStruct
			{
				A = double.MinValue,
				B = int.MaxValue,
				C = null
			};
			_serializer.RoundtripObject(value).Should().Be(value);
		}

		[Test]
		public void TestRoundtripIntArray()
		{
			_serializer.RegisterType<int[]>();
			_serializer.RoundtripValue(new int[0]);
			_serializer.RoundtripValue(new[] { 42 }).Should().Equal(new[] { 42 });
			_serializer.RoundtripValue(new[] { int.MinValue, int.MaxValue }).Should().Equal(new[] { int.MinValue, int.MaxValue });
			_serializer.RoundtripValue(new[] { -1, 0, 42, 9001 }).Should().Equal(new[] { -1, 0, 42, 9001 });
		}

		[Test]
		public void TestRoundtripStringArray()
		{
			_serializer.RegisterType<string[]>();
			_serializer.RoundtripValue(new string[0]);
			_serializer.RoundtripValue(new[] { "Foobar" }).Should().Equal(new[] { "Foobar" });
			_serializer.RoundtripValue(new[] { "a", "b" }).Should().Equal(new[] { "a", "b" });
			_serializer.RoundtripValue(new[] { "a", null, "b" }).Should().Equal(new[] { "a", null, "b" });
		}

		[Test]
		public void TestRoundtripFieldStructArray()
		{
			_serializer.RegisterType<FieldStruct[]>();

			_serializer.RoundtripValue(new FieldStruct[0]).Should().Equal(new FieldStruct[0]);
			var values = new[]
				{
					new FieldStruct
						{
							A = 42,
							B = -1223112,
							C = "Sunday!"
						}
				};
			_serializer.RoundtripValue(values).Should().Equal(values);
		}
	}
}