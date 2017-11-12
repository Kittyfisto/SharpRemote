using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Xml
{
	[TestFixture]
	public sealed class XmlSerializerTest
	{
		public static IEnumerable<byte> Bytes => Enumerable.Range(0, byte.MaxValue+1).Select(i => (byte)i).ToList();

		[Test]
		public void TestHexEncoding1([ValueSource(nameof(Bytes))] byte value)
		{
			var hexString = XmlSerializer.HexFromBytes(new[] {value});
			TestContext.Out.WriteLine(hexString);
			var actualValue = XmlSerializer.BytesFromHex(hexString);
			actualValue.Should().NotBeNull();
			actualValue.Should().HaveCount(1);
			actualValue[0].Should().Be(value);
		}

		[Test]
		public void TestHexEncoding2([Values(0, 1, 2, 4, 8, 16)] int length)
		{
			int seed = Environment.TickCount;
			TestContext.Out.WriteLine("Seed: {0}", seed);
			var rng = new Random(seed);
			var value = new byte[length];
			rng.NextBytes(value);

			var hexString = XmlSerializer.HexFromBytes(value);
			TestContext.Out.WriteLine(hexString);
			var actualValue = XmlSerializer.BytesFromHex(hexString);
			actualValue.Should().NotBeNull();
			actualValue.Should().Equal(value);
		}
	}
}