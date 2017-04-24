using FluentAssertions;
using NUnit.Framework;
using SharpRemote.WebApi;

namespace SharpRemote.Test.WebApi.Attributes
{
	[TestFixture]
	public sealed class HttpPutAttributeTest
	{
		[Test]
		public void TestConstruct1()
		{
			var attribute = new HttpPutAttribute();
			attribute.Route.Should().BeNull();
			attribute.Method.Should().Be(HttpMethod.Put);
		}

		[Test]
		public void TestConstruct2()
		{
			var attribute = new HttpPutAttribute(@"{0}\{1}");
			attribute.Route.Should().Be(@"{0}\{1}");
			attribute.Method.Should().Be(HttpMethod.Put);
		}
	}
}