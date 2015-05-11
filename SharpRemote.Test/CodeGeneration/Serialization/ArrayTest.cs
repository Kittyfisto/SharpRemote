using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestRoundtripIntArray()
		{
			_serializer.RegisterType<int[]>();
			_serializer.RoundtripValue(new int[0]);
			_serializer.RoundtripValue(new[] {42}).Should().Equal(new[] {42});
			_serializer.RoundtripValue(new[] {int.MinValue, int.MaxValue}).Should().Equal(new[] {int.MinValue, int.MaxValue});
			_serializer.RoundtripValue(new[] {-1, 0, 42, 9001}).Should().Equal(new[] {-1, 0, 42, 9001});
		}

		[Test]
		public void TestRoundtripStringArray()
		{
			_serializer.RegisterType<string[]>();
			_serializer.RoundtripValue(new string[0]);
			_serializer.RoundtripValue(new[] {"Foobar"}).Should().Equal(new[] {"Foobar"});
			_serializer.RoundtripValue(new[] {"a", "b"}).Should().Equal(new[] {"a", "b"});
			_serializer.RoundtripValue(new[] {"a", null, "b"}).Should().Equal(new[] {"a", null, "b"});
		}

		[Test]
		public void TestRoundtripPropertySealedClassArray()
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
			_serializer.RoundtripValue(value)
			           .Should().Equal(value);
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