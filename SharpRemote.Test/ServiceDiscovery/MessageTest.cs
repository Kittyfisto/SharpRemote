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
			new byte[10]

			// Doesn't work (yet)
			//new byte[1] {1}
		};

		[Test]
		[Description("Verifies that TryRead doesn't throw when the stream is too small or contains gibberish data")]
		public void TestTryRead1([ValueSource(nameof(InvalidMessages))] byte[] message)
		{
			string token;
			string name;
			IPEndPoint endPoint;
			Message.TryRead(message, out token, out name, out endPoint).Should().BeFalse();
		}
	}
}
