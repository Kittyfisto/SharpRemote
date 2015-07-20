using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public sealed class TypeLoaderTest
	{
		[Test]
		[Description("Verifies that the specified custom type resolver is called upon deserialization")]
		public void TestCustomTypeResolver1()
		{
			var resolver = new CustomTypeResolver1();
			var serializer = new Serializer(resolver);
			serializer.ShouldRoundtrip(typeof (int));
			resolver.GetTypeCalled.Should().Be(2, "Because the custom type resolve method should be used once to determine that a Type object is being deserialized and then again to retrieve the actual type value");
		}

		[Test]
		[Description("Verifies that the specified custom type resolver is used to actually look up the correct deserializer for a type")]
		public void TestCustomTypeResolver2()
		{
			var resolver = new CustomTypeResolver2(name =>
				{
					name.Should().Be("Birke");
					return typeof (Birke);
				});

			var serializer = new Serializer(resolver);
			serializer.RegisterType<Birke>();

			using (var stream = new MemoryStream())
			using (var writer =new BinaryWriter(stream))
			{
				writer.Write("Birke"); //< Instead of specifying the entire typename, we specify a part
				// of it and then use the type resolver to correct it. This ensures that the type resolver
				// is actually used (and not just accidentally called or something).

				writer.Write(true);
				writer.Write("Hello Pluto <3");

				writer.Write((byte)128);

				writer.Write(42.0);

				writer.Flush();
				stream.Position = 0;

				var tree = serializer.ReadObject(new BinaryReader(stream), null);
				tree.Should().BeOfType<Birke>();
				var birke = (Birke)tree;
				birke.A.Should().Be(42.0);
				birke.B.Should().Be(128);
				birke.C.Should().Be("Hello Pluto <3");
			}
		}
	}
}