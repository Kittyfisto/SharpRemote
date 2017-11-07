using System;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class WriteTest
	{
		[SetUp]
		public void SetUp()
		{
			_data = new MemoryStream();
			_reader = new BinaryReader(_data);
			_writer = new BinaryWriter(_data);
		}

		private ISerializer _serializer;
		private MemoryStream _data;
		private BinaryReader _reader;
		private BinaryWriter _writer;

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			_serializer = new BinarySerializer();
		}

		[Test]
		[Description("Verifies the binary output of serializing a null value")]
		public void TestWriteNull()
		{
			_serializer.WriteObject(_writer, null, null);
			_data.Position = 0;

			_reader.ReadString().Should().Be("null");
			_data.Position.Should().Be(_data.Length);
		}

		[Test]
		public void TestWriteObjectFieldWithString()
		{
			var value = new FieldObjectStruct {Value = "I'm your father, Luke"};
			_serializer.WriteObject(_writer, value, null);
			_data.Position = 0;

			_reader.ReadString().Should().Be(typeof (FieldObjectStruct).AssemblyQualifiedName);
			_reader.ReadString().Should().Be(typeof (string).AssemblyQualifiedName);
			_reader.ReadString().Should().Be("I'm your father, Luke");

			_data.Position.Should().Be(_data.Length);
		}

		[Test]
		[Description("Verifies the binary output of serializing a type object")]
		public void TestWriteType()
		{
			_serializer.WriteObject(_writer, typeof (int), null);
			_data.Position = 0;

			_reader.ReadString().Should().Be(typeof (Type).AssemblyQualifiedName);
			_reader.ReadString().Should().Be(typeof (int).AssemblyQualifiedName);
			_data.Position.Should().Be(_data.Length);
		}
	}
}