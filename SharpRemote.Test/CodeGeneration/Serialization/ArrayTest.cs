using System;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestByteArray()
		{
			_serializer.RegisterType<byte[]>();
			_serializer.ShouldRoundtripEnumeration(new byte[0]);
			_serializer.ShouldRoundtripEnumeration(new byte[] {42});
			_serializer.ShouldRoundtripEnumeration(new[] {byte.MinValue, byte.MaxValue});
			_serializer.ShouldRoundtripEnumeration(new byte[] {1, 0, 42, 244});
		}

		[Test]
		public void TestFieldStructArray()
		{
			_serializer.RegisterType<FieldStruct[]>();

			_serializer.ShouldRoundtripEnumeration(new FieldStruct[0]);
			var values = new[]
				{
					new FieldStruct
						{
							A = 42,
							B = -1223112,
							C = "Sunday!"
						}
				};
			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestIntArray()
		{
			_serializer.RegisterType<int[]>();
			_serializer.ShouldRoundtripEnumeration(new int[0]);
			_serializer.ShouldRoundtripEnumeration(new[] {42});
			_serializer.ShouldRoundtripEnumeration(new[] {int.MinValue, int.MaxValue});
			_serializer.ShouldRoundtripEnumeration(new[] {-1, 0, 42, 9001});
		}

		[Test]
		public void TestPropertySealedClassArray()
		{
			_serializer.RegisterType<PropertySealedClass[]>();

			var value = new[]
				{
					new PropertySealedClass
						{
							Value1 = "Executor",
							Value2 = -31231,
							Value3 = 31231.131231
						},
					null,
					new PropertySealedClass
						{
							Value1 = "Yes, my master",
							Value2 = 088312,
							Value3 = Math.E
						},
					null
				};
			_serializer.ShouldRoundtripEnumeration(value);
		}

		[Test]
		public void TestStringArray()
		{
			_serializer.RegisterType<string[]>();
			_serializer.ShouldRoundtripEnumeration(new string[0]);
			_serializer.ShouldRoundtripEnumeration(new[] {"Foobar"});
			_serializer.ShouldRoundtripEnumeration(new[] {"a", "b"});
			_serializer.ShouldRoundtripEnumeration(new[] {"a", null, "b"});
		}
	}
}