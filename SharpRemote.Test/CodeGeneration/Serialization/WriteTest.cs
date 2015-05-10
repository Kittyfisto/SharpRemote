using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class WriteTest
	{
		private ISerializer _serializer;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			_serializer =new Serializer();
		}

		[Test]
		[Description("Verifies the binary output of serializing a type object")]
		public void TestWriteType()
		{
			var data = new MemoryStream();
			var reader = new BinaryReader(data);
			_serializer.WriteObject(new BinaryWriter(data), typeof(int));
			data.Position = 0;

			reader.ReadString().Should().Be(typeof (int).GetType().AssemblyQualifiedName);
			// TODO: this instance information should be removed in the future as it's redundant
			reader.ReadBoolean().Should().BeTrue();
			reader.ReadString().Should().Be(typeof (int).AssemblyQualifiedName);
		}
	}
}