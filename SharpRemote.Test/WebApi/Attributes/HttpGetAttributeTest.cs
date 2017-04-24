using FluentAssertions;
using NUnit.Framework;
using SharpRemote.WebApi;

namespace SharpRemote.Test.WebApi.Attributes
{
	[TestFixture]
	public sealed class HttpGetAttributeTest
	{
		[Test]
		public void TestConstruct1()
		{
			var attribute = new HttpGetAttribute();
			attribute.Route.Should().BeNull();
			attribute.Method.Should().Be(HttpMethod.Get);
		}

		[Test]
		public void TestConstruct2()
		{
			var attribute = new HttpGetAttribute(@"{0}\{1}");
			attribute.Route.Should().Be(@"{0}\{1}");
			attribute.Method.Should().Be(HttpMethod.Get);
		}
	}
}