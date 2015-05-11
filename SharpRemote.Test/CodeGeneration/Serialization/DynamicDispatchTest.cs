using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestFieldObjectStructWithNull()
		{
			var value = new FieldObjectStruct { Value = null };
			_serializer.RoundtripObject(value).Should().Be(value);
		}

		[Test]
		public void TestFieldObjectStructWithString()
		{
			var value = new FieldObjectStruct { Value = "I'm your father, Luke" };
			_serializer.RoundtripObject(value).Should().Be(value);
		}

		[Test]
		public void TestFieldObjectStructWithDouble()
		{
			var value = new FieldObjectStruct { Value = Math.PI };
			_serializer.RoundtripObject(value).Should().Be(value);
		}

		[Test]
		public void TestFieldObjectStructWithStruct()
		{
			var value = new FieldObjectStruct { Value = new FieldObjectStruct{Value = 42} };
			_serializer.RoundtripObject(value).Should().Be(value);
		}
	}
}