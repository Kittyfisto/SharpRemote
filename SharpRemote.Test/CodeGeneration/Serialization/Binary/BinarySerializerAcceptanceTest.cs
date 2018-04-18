using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;

namespace SharpRemote.Test.CodeGeneration.Serialization.Binary
{
	[TestFixture]
	public sealed class BinarySerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		protected override ISerializer2 Create()
		{
			return new BinarySerializer2();
		}

		protected override void Save()
		{
			
		}

		public static IEnumerable<ProtocolVersion> SupportedVersions => new[]
		{
			ProtocolVersion.None,
			ProtocolVersion.Version1
		};

		public static IEnumerable<Serializer> SupportedSerializer => new[]
		{
			Serializer.None,
			Serializer.BinarySerializer,
			Serializer.XmlSerializer,
			Serializer.BinarySerializer | Serializer.XmlSerializer
		};

		public static IEnumerable<object> Challenges => new object[]
		{
			null,
			42,
			"Where is Waldo?",
			new Birke
			{
				A = Math.PI,
				B = byte.MaxValue,
				C = "Where's my money?"
			}
		};

		[Test]
		[Ignore("Not implemented yet")]
		public void TestSerializeHandshakeSync(
			[ValueSource(nameof(SupportedVersions))] ProtocolVersion supportedVersions,
			[ValueSource(nameof(SupportedSerializer))] Serializer supportedSerializers,
			[ValueSource(nameof(Challenges))] object challenge
			)
		{
			var message = new HandshakeSyn
			{
				SupportedVersions = supportedVersions,
				SupportedSerializers = supportedSerializers,
				Challenge = challenge
			};
			var actualMessage = Roundtrip(message);
			actualMessage.SupportedVersions.Should().Be(supportedVersions);
			actualMessage.SupportedSerializers.Should().Be(supportedSerializers);
			actualMessage.Challenge.Should().Be(challenge);
		}

		private T Roundtrip<T>(T message)
		{
			var serializer = (BinarySerializer2)Create();
			var serializedMessage = serializer.SerializeWithoutTypeInformation(message);
			var actualMessage = serializer.Deserialize<T>(serializedMessage);
			return actualMessage;
		}

		protected override string Format(MemoryStream stream)
		{
			var value = stream.ToArray();
			var stringBuilder = new StringBuilder(value.Length * 2);
			foreach (var b in value)
				stringBuilder.AppendFormat("{0:x2}", b);
			return stringBuilder.ToString();
		}
	}
}