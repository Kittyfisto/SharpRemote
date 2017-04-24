using FluentAssertions;
using NUnit.Framework;
using SharpRemote.WebApi;

namespace SharpRemote.Test.WebApi.Attributes
{
	[TestFixture]
	public sealed class HttpPatchAttributeTest
	{
		[Test]
		public void TestConstruct1()
		{
			var attribute = new HttpPatchAttribute();
			attribute.Route.Should().BeNull();
			attribute.Method.Should().Be(HttpMethod.Patch);
		}

		[Test]
		public void TestConstruct2()
		{
			var attribute = new HttpPatchAttribute(@"{0}\{1}");
			attribute.Route.Should().Be(@"{0}\{1}");
			attribute.Method.Should().Be(HttpMethod.Patch);
		}
	}
}