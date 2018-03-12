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
		public void TestConstruction()
		{
			var exception = new ConnectionLostException();
			exception.Message.Should().Be("The connection to the remote endpoint has been lost");
			exception.InnerException.Should().BeNull();
		}

		[Test]
		[SetCulture("en-US")]
		public void TestRoundtrip()
		{
			var exception = new ConnectionLostException();
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be("The connection to the remote endpoint has been lost");
		}
	}
}
