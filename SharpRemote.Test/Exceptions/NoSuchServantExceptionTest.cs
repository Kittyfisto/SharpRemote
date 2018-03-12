using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class NoSuchServantExceptionTest
		: AbstractExceptionTest<NoSuchServantException>
	{
		[Test]
		public void TestConstruction()
		{
			const ulong grainId = 42;
			var exception = new NoSuchServantException(grainId);
			exception.Message.Should().Be(string.Format("No such servant: {0}", grainId));
		}

		[Test]
		public void TestSerializationRoundtrip()
		{
			const ulong grainId = 42;
			var exception = new NoSuchServantException(grainId);
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be(string.Format("No such servant: {0}", grainId));
		}
	}
}