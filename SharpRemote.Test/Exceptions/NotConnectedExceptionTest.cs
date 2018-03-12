using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class NotConnectedExceptionTest
		: AbstractExceptionTest<NotConnectedException>
	{
		[Test]
		public void TestConstruction()
		{
			var endPointName = "Some endpoint";
			var exception = new NotConnectedException(endPointName);
			exception.Message.Should().Be("This endpoint is not connected to any other endpoint");
			exception.EndPointName.Should().Be(endPointName);
		}

		[Test]
		[SetCulture("en-US")]
		public void TestRoundtrip()
		{
			var endPointName = "Some endpoint";
			var exception = new NotConnectedException(endPointName);
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be("This endpoint is not connected to any other endpoint");
			actualException.EndPointName.Should().Be(endPointName);
		}
	}
}