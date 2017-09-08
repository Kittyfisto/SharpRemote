using System.Net;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;
using System.Collections.Generic;

namespace SharpRemote.Test.ServiceDiscovery
{
	[TestFixture]
	public sealed class MessageTest
	{
		public static IEnumerable<byte[]> InvalidMessages => new[]
		{
			new byte[0],
			new byte[3],
			new byte[10],
			new byte[] {1},
			new byte[] {1, 0,0,0, 41}
		};

		[Test]
		[Description("Verifies that a response consisting of just name and endpoint can still be parsed")]
		public void TestResponseBackwardsCompatibility1()
		{
			var response = new byte[] { 0x18, 0x53, 0x68, 0x61, 0x72, 0x70, 0x52, 0x65, 0x6D, 0x6F, 0x74, 0x65, 0x2E, 0x50, 0x32, 0x50, 0x2E, 0x52, 0x65, 0x73, 0x70, 0x6F, 0x6E, 0x73, 0x65, 0x07, 0x53, 0x6F, 0x6D, 0x65, 0x41, 0x70, 0x70, 0x04, 0x52, 0x00, 0x76, 0x59, 0xD1, 0xD9, 0x00, 0x00, 0xC2, 0x71, 0xDC, 0xDA, 0x8B, 0x46, 0x53, 0xCD, 0x10, 0x52, 0x34, 0x9C, 0xE7, 0xAE, 0x04, 0x62 };

			string token;
			string name;
			IPEndPoint endPoint;
			string payload;
			Message.TryRead(response, out token, out name, out endPoint, out payload).Should().BeTrue();
			token.Should().Be(Message.P2PResponseLegacyToken);
			name.Should().Be("SomeApp");
			endPoint.Should().Be(new IPEndPoint(IPAddress.Parse("82.0.118.89"), 55761));
			payload.Should().BeNull("because this message didn't contain any payload");
		}

		[Test]
		[Description("Verifies that TryRead doesn't throw when the stream is too small or contains gibberish data")]
		public void TestTryRead1([ValueSource(nameof(InvalidMessages))] byte[] message)
		{
			string token;
			string name;
			IPEndPoint endPoint;
			string payload;
			Message.TryRead(message, out token, out name, out endPoint, out payload).Should().BeFalse();
			name.Should().BeNull();
			endPoint.Should().BeNull();
			payload.Should().BeNull();
		}

		[Test]
		public void TestQueryRoundtrip()
		{
			var data = Message.CreateQuery("foobar");
			string token;
			string name;
			IPEndPoint endPoint;
			string payload;
			Message.TryRead(data, out token, out name, out endPoint, out payload)
				.Should().BeTrue();
			token.Should().Be(Message.P2PQueryToken);
			name.Should().Be("foobar");
			endPoint.Should().BeNull();
			payload.Should().BeNull();
		}

		[Test]
		public void TestLegacyResponseRoundtrip1()
		{
			var data = Message.CreateLegacyResponse("foo", new IPEndPoint(IPAddress.Parse("192.168.1.10"), 1234));
			string token;
			string name;
			IPEndPoint endPoint;
			string payload;
			Message.TryRead(data, out token, out name, out endPoint, out payload)
				.Should().BeTrue();

			token.Should().Be(Message.P2PResponseLegacyToken);
			name.Should().Be("foo");
			endPoint.Should().Be(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 1234));
			payload.Should().BeNull();
		}

		[Test]
		[Description("Verifies that a response V2 without payload roundtrips")]
		public void TestResponse2Roundtrip1()
		{
			var data = Message.CreateResponse2("foo", new IPEndPoint(IPAddress.Parse("192.168.1.10"), 1234));
			string token;
			string name;
			IPEndPoint endPoint;
			string payload;
			Message.TryRead(data, out token, out name, out endPoint, out payload)
				.Should().BeTrue();

			token.Should().Be(Message.P2PResponse2Token);
			name.Should().Be("foo");
			endPoint.Should().Be(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 1234));
			payload.Should().BeNull();
		}

		[Test]
		[Description("Verifies that a response V2 with payload roundtrips")]
		public void TestResponse2Roundtrip2()
		{
			var data = Message.CreateResponse2("foo", new IPEndPoint(IPAddress.Parse("192.168.1.10"), 1234), "hello, world!");
			string token;
			string name;
			IPEndPoint endPoint;
			string payload;
			Message.TryRead(data, out token, out name, out endPoint, out payload)
				.Should().BeTrue();

			token.Should().Be(Message.P2PResponse2Token);
			name.Should().Be("foo");
			endPoint.Should().Be(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 1234));
			payload.Should().Be("hello, world!");
		}
	}
}
