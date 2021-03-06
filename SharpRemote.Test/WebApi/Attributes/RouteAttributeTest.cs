﻿using FluentAssertions;
using NUnit.Framework;
using SharpRemote.WebApi;

namespace SharpRemote.Test.WebApi.Attributes
{
	[TestFixture]
	public sealed class RouteAttributeTest
	{
		[Test]
		public void TestConstruct1()
		{
			var attribute = new RouteAttribute();
			attribute.Template.Should().BeNull();
		}

		[Test]
		public void TestConstruct2()
		{
			var attribute = new RouteAttribute(@"{0}\{1}");
			attribute.Template.Should().Be(@"{0}\{1}");
		}
	}
}