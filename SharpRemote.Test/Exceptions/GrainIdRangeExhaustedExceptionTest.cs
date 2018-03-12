using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class GrainIdRangeExhaustedExceptionTest
		: AbstractExceptionTest<GrainIdRangeExhaustedException>
	{
		[Test]
		public void TestConstruction()
		{
			var exception = new GrainIdRangeExhaustedException();
			exception.Message.Should().Be("The range of available grain ids has been exhausted - no more can be generated");
			exception.InnerException.Should().BeNull();
		}

		[Test]
		[SetCulture("en-US")]
		public void TestRoundtrip()
		{
			var exception = new GrainIdRangeExhaustedException();
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be("The range of available grain ids has been exhausted - no more can be generated");
		}
	}
}