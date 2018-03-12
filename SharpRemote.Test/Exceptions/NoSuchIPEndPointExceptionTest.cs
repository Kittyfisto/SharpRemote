using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class NoSuchIPEndPointExceptionTest
		: AbstractExceptionTest<NoSuchIPEndPointException>
	{
		[Test]
		public void TestConstruction()
		{
			var endpointName = "Some server";
			var innerException = new ArgumentException("dawdwdw");
			var exception = new NoSuchIPEndPointException(endpointName, innerException);
			exception.Message.Should().Be(string.Format("Unable to establish a connection with the given endpoint: {0}", endpointName));
			exception.InnerException.Should().BeSameAs(innerException);
		}

		[Test]
		public void TestSerializationRoundtrip()
		{
			var innerException = new ArgumentException("dawdwdw");
			var endpointName = "Some server";
			var exception = new NoSuchIPEndPointException(endpointName, innerException);
			var actualException = exception.Roundtrip();
			exception.Message.Should().Be(string.Format("Unable to establish a connection with the given endpoint: {0}", endpointName));
			actualException.InnerException.Should().BeOfType<ArgumentException>();
			actualException.InnerException.Message.Should().Be("dawdwdw");
		}
	}
}