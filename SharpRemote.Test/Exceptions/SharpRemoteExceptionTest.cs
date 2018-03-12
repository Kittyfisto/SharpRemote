using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class SharpRemoteExceptionTest
		: AbstractExceptionTest<SharpRemoteException>
	{
		[Test]
		public void TestConstruction()
		{
			var message = "Some error occured";
			var innerException = new ArgumentNullException("Somebody screwed up");
			var exception = new SharpRemoteException(message, innerException);
			exception.Message.Should().Be(message);
			exception.InnerException.Should().Be(innerException);
		}

		[Test]
		[SetCulture("en-US")]
		public void TestRoundtrip()
		{
			var message = "Some error occured";
			var innerException = new ArgumentNullException("foobar", "Somebody screwed up");
			var exception = new SharpRemoteException(message, innerException);
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be(message);
			actualException.InnerException.Should().BeOfType<ArgumentNullException>();
			actualException.InnerException.Message.Should().Be("Somebody screwed up\r\nParameter name: foobar");
		}
	}
}