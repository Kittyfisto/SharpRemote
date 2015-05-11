using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Structs;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class WriteTest
	{
		private ISerializer _serializer;
		private AssemblyBuilder _assembly;
		private string _moduleName;
		private MemoryStream _data;
		private BinaryReader _reader;
		private BinaryWriter _writer;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var assemblyName = new AssemblyName("SharpRemote.GeneratedCode.Serializer");
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

		[SetUp]
		public void SetUp()
		{
			_data = new MemoryStream();
			_reader = new BinaryReader(_data);
			_writer = new BinaryWriter(_data);
		}

		[Test]
		[Description("Verifies the binary output of serializing a type object")]
		public void TestWriteType()
		{
			_serializer.WriteObject(_writer, typeof(int));
			_data.Position = 0;

			_reader.ReadString().Should().Be(typeof(int).GetType().AssemblyQualifiedName);
			_reader.ReadString().Should().Be(typeof(int).AssemblyQualifiedName);
			_data.Position.Should().Be(_data.Length);
		}

		[Test]
		public void TestWriteObjectFieldWithString()
		{
			var value = new FieldObjectStruct { Value = "I'm your father, Luke" };
			_serializer.WriteObject(_writer, value);
			_data.Position = 0;

			_reader.ReadString().Should().Be(typeof (FieldObjectStruct).AssemblyQualifiedName);
			_reader.ReadString().Should().Be(typeof (string).AssemblyQualifiedName);
			_reader.ReadString().Should().Be("I'm your father, Luke");

			_data.Position.Should().Be(_data.Length);
		}

		[Test]
		[Description("Verifies the binary output of serializing a null value")]
		public void TestWriteNull()
		{
			_serializer.WriteObject(_writer, null);
			_data.Position = 0;

			_reader.ReadString().Should().Be("null");
			_data.Position.Should().Be(_data.Length);
		}
	}
}