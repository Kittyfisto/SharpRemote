using System;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.Exceptions
{
	public abstract class AbstractExceptionTest<T>
		where T : Exception
	{
		[Test]
		[Description("Ensures that the exception lies inside the SharpRemote namespace")]
		public void TestNamespace()
		{
			var type = typeof(T);
			type.Namespace.Should().Be("SharpRemote");
		}
	}
}