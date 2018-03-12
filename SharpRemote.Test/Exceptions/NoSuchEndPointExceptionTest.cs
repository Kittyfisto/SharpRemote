using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class NoSuchEndPointExceptionTest
		: AbstractExceptionTest<NoSuchEndPointException>
	{
		[Test]
		public void TestConstruction()
		{
			var innerException = new ArgumentException("dawdwdw");
			var exception = new NoSuchEndPointException("foobar", innerException);
			exception.Message.Should().Be("foobar");
			exception.InnerException.Should().BeSameAs(innerException);
		}

		[Test]
		public void TestSerializationRoundtrip()
		{
			var innerException = new ArgumentException("dawdwdw");
			var message = "Some error message";
			var exception = new NoSuchEndPointException(message, innerException);
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be(message);
			actualException.InnerException.Should().BeOfType<ArgumentException>();
			actualException.InnerException.Message.Should().Be("dawdwdw");
		}
	}
}