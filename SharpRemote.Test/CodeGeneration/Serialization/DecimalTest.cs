using System;
using NUnit.Framework;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestDecimal1()
		{
			_serializer.RegisterType<decimal>();

			var values = new[]
				{
					0,
					Math.E,
					Math.PI,
					42,
					9001,
					-10000,
					int.MinValue,
					int.MaxValue,
					long.MinValue,
					long.MaxValue,
				};

			foreach (var value in values)
			{
				_serializer.ShouldRoundtrip(new decimal(value));
			}

			_serializer.ShouldRoundtrip(decimal.Zero);
			_serializer.ShouldRoundtrip(decimal.MinValue);
			_serializer.ShouldRoundtrip(decimal.MaxValue);
		}

		[Test]
		[Description("Verifies that serialization preserves all 28 significant digits that decimal can represent")]
		public void TestDecimal2()
		{
			_serializer.ShouldRoundtrip(decimal.Parse("3.1415926535897932384626433832"));
		}

		[Test]
		public void TestStructWithDecimal()
		{
			var value = new FieldDecimal
				{
					Value = new decimal(Math.PI)
				};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestStructWithOptionalDecimal()
		{
			var value = new FieldOptionalDecimal
			{
				Value = new decimal(Math.PI)
			};
			_serializer.ShouldRoundtrip(value);

			value = new FieldOptionalDecimal
				{
					Value = null
				};
			_serializer.ShouldRoundtrip(value);
		}
	}
}