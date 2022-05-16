using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class RemoteEndpointAlreadyConnectedExceptionTest
		: AbstractExceptionTest<RemoteEndpointAlreadyConnectedException>
	{
		[Test]
		public void TestConstruction()
		{
			var endPoint = new IPEndPoint(IPAddress.Parse("192.21.32.42"), 54321);
			var innerException = new ArgumentException("dawdwdw");
			var exception = new RemoteEndpointAlreadyConnectedException("foobar", endPoint.ToString(), innerException);
			exception.Message.Should().Be("foobar");
			exception.BlockingEndPointName.Should().Be(endPoint.ToString());
			exception.InnerException.Should().BeSameAs(innerException);
		}

		[Test]
		public void TestSerializationRoundtrip()
		{
			var endPoint = new IPEndPoint(IPAddress.Parse("192.21.32.42"), 54321);
			var innerException = new ArgumentException("dawdwdw");
			var message = "Some error message";
			var exception = new RemoteEndpointAlreadyConnectedException(message, endPoint.ToString(), innerException);
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be(message);
			actualException.BlockingEndPointName.Should().Be(endPoint.ToString());
			actualException.InnerException.Should().BeOfType<ArgumentException>();
			actualException.InnerException.Message.Should().Be("dawdwdw");
		}
	}
}