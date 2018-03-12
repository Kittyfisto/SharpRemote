using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class NoSuchApplicationExceptionTest
		: AbstractExceptionTest<NoSuchApplicationException>
	{
		[Test]
		public void TestConstruction()
		{
			var applicationName = "Hello, World!";
			var innerException = new ArgumentException("dawdwdw");
			var exception = new NoSuchApplicationException(applicationName, innerException);
			exception.Message.Should().Be(string.Format("There is no installed application with this name: {0}", applicationName));
			exception.InnerException.Should().BeSameAs(innerException);
		}

		[Test]
		public void TestSerializationRoundtrip()
		{
			var applicationName = "Hello, World!";
			var innerException = new ArgumentException("dawdwdw");
			var exception = new NoSuchApplicationException(applicationName, innerException);
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be(string.Format("There is no installed application with this name: {0}", applicationName));
			actualException.InnerException.Should().BeOfType<ArgumentException>();
			actualException.InnerException.Message.Should().Be("dawdwdw");
		}
	}
}