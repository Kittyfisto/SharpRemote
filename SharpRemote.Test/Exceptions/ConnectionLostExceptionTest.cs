using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class ConnectionLostExceptionTest
		: AbstractExceptionTest<ConnectionLostException>
	{
		[Test]
		public void TestConstruction1()
		{
			var exception = new ConnectionLostException();
			exception.Message.Should().Be("The connection to the remote endpoint has been lost");
			exception.EndPointName.Should().BeNull("because none has been specified in the ctor");
			exception.InnerException.Should().BeNull("because none has been specified in the ctor");
		}

		[Test]
		public void TestConstruction2()
		{
			var exception = new ConnectionLostException("My fancy endpoint");
			exception.Message.Should().Be("The connection to the remote endpoint has been lost");
			exception.EndPointName.Should().Be("My fancy endpoint");
			exception.InnerException.Should().BeNull("because none has been specified in the ctor");
		}

		[Test]
		[SetCulture("en-US")]
		public void TestRoundtrip1()
		{
			var exception = new ConnectionLostException();
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be("The connection to the remote endpoint has been lost");
			actualException.EndPointName.Should().BeNull();
		}

		[Test]
		[SetCulture("en-US")]
		public void TestRoundtrip2()
		{
			var exception = new ConnectionLostException("My fancy endpoint");
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be("The connection to the remote endpoint has been lost");
			actualException.EndPointName.Should().Be("My fancy endpoint");
		}
	}
}