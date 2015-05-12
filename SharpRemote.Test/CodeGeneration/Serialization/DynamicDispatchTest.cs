using System;
using NUnit.Framework;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestFieldObjectStructWithDouble()
		{
			var value = new FieldObjectStruct {Value = Math.PI};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestFieldObjectStructWithNull()
		{
			var value = new FieldObjectStruct {Value = null};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestFieldObjectStructWithString()
		{
			var value = new FieldObjectStruct {Value = "I'm your father, Luke"};
			_serializer.ShouldRoundtrip(value);
		}

		[Test]
		public void TestFieldObjectStructWithStruct()
		{
			var value = new FieldObjectStruct {Value = new FieldObjectStruct {Value = 42}};
			_serializer.ShouldRoundtrip(value);
		}
	}
}