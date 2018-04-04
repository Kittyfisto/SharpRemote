using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Extensions;

namespace SharpRemote.Test.Exceptions
{
	[TestFixture]
	public sealed class RemoteProcedureCallCanceledExceptionTest
		: AbstractExceptionTest<RemoteProcedureCallCanceledException>
	{
		[Test]
		public void TestConstruction()
		{
			var exception = new RemoteProcedureCallCanceledException();
			exception.Message.Should().Be("The remote procedure call has been canceled");
			exception.InnerException.Should().BeNull();
		}

		[Test]
		[SetCulture("en-US")]
		public void TestRoundtrip()
		{
			var exception = new RemoteProcedureCallCanceledException();
			var actualException = exception.Roundtrip();
			actualException.Message.Should().Be("The remote procedure call has been canceled");
		}
	}
}