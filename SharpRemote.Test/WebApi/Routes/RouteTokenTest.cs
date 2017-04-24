using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.WebApi.Routes;

namespace SharpRemote.Test.WebApi.Routes
{
	[TestFixture]
	public sealed class RouteTokenTest
	{
		[Test]
		public void TestTokenize1()
		{
			RouteToken.Tokenize(null).Should().BeEmpty();
			RouteToken.Tokenize("").Should().BeEmpty();
		}

		[Test]
		public void TestTokenize2()
		{
			new Action(() => RouteToken.Tokenize("{")).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestTokenize3()
		{
			new Action(() => RouteToken.Tokenize("{0")).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestTokenize4()
		{
			new Action(() => RouteToken.Tokenize("{0{")).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestTokenize5()
		{
			new Action(() => RouteToken.Tokenize("{-1}")).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestTokenize6()
		{
			new Action(() => RouteToken.Tokenize("{0xFAAAA}")).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestTokenize7()
		{
			new Action(() => RouteToken.Tokenize("{AB}")).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestTokenize8()
		{
			new Action(() => RouteToken.Tokenize("{}")).ShouldThrow<ArgumentException>();
		}

		[Test]
		public void TestTokenize9()
		{
			RouteToken.Tokenize("A").Should().Equal(RouteToken.Constant("A"));
			RouteToken.Tokenize("AB").Should().Equal(RouteToken.Constant("AB"));
			RouteToken.Tokenize("ABC").Should().Equal(RouteToken.Constant("ABC"));
		}

		[Test]
		public void TestTokenize10()
		{
			RouteToken.Tokenize("{0}").Should().Equal(RouteToken.Argument(0));
			RouteToken.Tokenize("{1}").Should().Equal(RouteToken.Argument(1));
			RouteToken.Tokenize("{10}").Should().Equal(RouteToken.Argument(10));
		}

		[Test]
		public void TestTokenize11()
		{
			RouteToken.Tokenize(@"{0}\{1}")
				.Should()
				.Equal(RouteToken.Argument(0),
					RouteToken.Constant(@"\"),
					RouteToken.Argument(1));
		}

		[Test]
		public void TestTokenize12()
		{
			RouteToken.Tokenize(@"id={0}")
				.Should()
				.Equal(
					RouteToken.Constant(@"id="),
					RouteToken.Argument(0));
		}

		[Test]
		public void TestEquals1()
		{
			RouteToken.Constant("A").Should().Be(RouteToken.Constant("A"));
			RouteToken.Constant("A").Should().NotBe(RouteToken.Constant("B"));
		}

		[Test]
		public void TestEquals2()
		{
			RouteToken.Argument(0).Should().Be(RouteToken.Argument(0));
			RouteToken.Argument(0).Should().NotBe(RouteToken.Argument(1));
			RouteToken.Argument(42).Should().Be(RouteToken.Argument(42));
			RouteToken.Argument(42).Should().NotBe(RouteToken.Argument(43));
		}
	}
}