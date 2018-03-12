using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class InstallationFailedExceptionTest
		: AbstractExceptionTest<InstallationFailedException>
	{
		[Test]
		public void TestConstruction()
		{
			var innerException = new ArgumentException("dawdwdw");
			var exception = new InstallationFailedException("foobar", innerException);
			exception.Message.Should().Be("foobar");
			exception.InnerException.Should().BeSameAs(innerException);
		}

		[Test]
		public void TestSerializationRoundtrip()
		{
			var innerException = new ArgumentException("dawdwdw");
			var message = "Some error message";
			var exception = new InstallationFailedException(message, innerException);
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be(message);
			actualException.InnerException.Should().BeOfType<ArgumentException>();
			actualException.InnerException.Message.Should().Be("dawdwdw");
		}
	}
}
