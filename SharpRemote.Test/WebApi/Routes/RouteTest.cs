using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.WebApi;
using SharpRemote.WebApi.Routes;

namespace SharpRemote.Test.WebApi.Routes
{
	[TestFixture]
	public sealed class RouteTest
	{
		[Test]
		public void TestConstruct1()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] {typeof(int)});
			route.Method.Should().Be(HttpMethod.Get);
		}

		public static IEnumerable<Type> Types => new[]
		{
			typeof(bool),
			typeof(byte),
			typeof(sbyte),
			typeof(ushort),
			typeof(short),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(uint)
		};

		[Test]
		public void TestTryMatch1([ValueSource(nameof(Types))] Type type)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { type });
			object[] values;
			route.TryMatch("", out values).Should().BeFalse();
			values.Should().BeNull();
		}

		[Test]
		public void TestTryMatchBool1([Values(true, false)] bool value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(bool) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}

		[Test]
		public void TestTryMatchBool2([Values(true, false)] bool value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(bool) });
			object[] values;
			route.TryMatch("tru", out values).Should().BeFalse();
			route.TryMatch("true!", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchSbyte1([Values(sbyte.MinValue, -100, -99, -10, -9, 0, 9, 10, 99, 100, sbyte.MaxValue)] sbyte value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(sbyte) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}

		[Test]
		public void TestTryMatchSbyte2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(sbyte) });
			object[] values;
			route.TryMatch("-129", out values).Should().BeFalse();
			route.TryMatch("128", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchShort1([Values(short.MinValue, -10000, -9999, -1000, -999, -100, -99, -10, -9, 0, 9, 10, 99, 100, 999, 1000, 9999, 10000, short.MaxValue)] short value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(short) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}

		[Test]
		public void TestTryMatchInt1([Values(int.MinValue, -1000000000, -999999999, -100000000, -99999999, -10000000, -9999999, -100000, -99999, -10000, -9999, -1000, -999, -100, -99, -10, -9, 0, 9, 10, 99, 100, 999, 1000, 9999, 10000, 99999, 100000, 999999, 1000000, 9999999, 10000000, 99999999, 1000000000, int.MaxValue)] int value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(int) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}
	}
}