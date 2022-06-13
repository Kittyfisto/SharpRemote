using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.WebApi;
using SharpRemote.WebApi.Routes;

namespace SharpRemote.WebApi.Test.Routes
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
		public void TestToString()
		{
			new Route(HttpMethod.Get, "stuff", new Type[0]).ToString().Should().Be("stuff");
			new Route(HttpMethod.Get, "{0}", new [] {typeof(int)}).ToString().Should().Be("{0}");
		}

		[Test]
		public void TestEquals1()
		{
			var route = new Route(HttpMethod.Get, "", new Type[0]);
			var equalRoute = new Route(HttpMethod.Get, "", new Type[0]);
			route.Equals(equalRoute).Should().BeTrue();
			route.GetHashCode().Should().Be(equalRoute.GetHashCode());
		}

		[Test]
		public void TestEquals2()
		{
			var route = new Route(HttpMethod.Get, "", new Type[0]);
			var differentRoute = new Route(HttpMethod.Put, "", new Type[0]);
			route.Equals(differentRoute).Should().BeFalse("because the two routes have a different method");
		}

		[Test]
		public void TestEquals3()
		{
			var route = new Route(HttpMethod.Put, "A", new Type[0]);
			var differentRoute = new Route(HttpMethod.Put, "", new Type[0]);
			route.Equals(differentRoute).Should().BeFalse("because the two routes have a different template");
		}

		[Test]
		public void TestEquals4()
		{
			var route = new Route(HttpMethod.Put, "{0}", new [] {typeof(int)});
			var differentRoute = new Route(HttpMethod.Put, "{0}", new [] {typeof(short)});
			route.Equals(differentRoute).Should().BeFalse("because the two routes have a different argument type");
		}

		[Test]
		public void TestEquals5()
		{
			var route = new Route(HttpMethod.Patch, "{0}/{1}", new [] {typeof(int), typeof(int)});
			var differentRoute = new Route(HttpMethod.Patch, "{0}", new[] {typeof(long)});
			route.Equals(differentRoute).Should().BeFalse("because the two routes have a different number of arguments");
		}

		[Test]
		public void TestEquals6()
		{
			var route = new Route(HttpMethod.Patch, "{0}/{1}", new[] {typeof(long), typeof(long)}, fromBodyIndex: 1);
			var differentRoute = new Route(HttpMethod.Patch, "{0}/{1}", new[] { typeof(long), typeof(long) }, fromBodyIndex: 0);
			route.Equals(differentRoute).Should().BeFalse("because the two routes extract a different argument from the body");
		}

		[Test]
		public void TestEquals7()
		{
			var route = new Route(HttpMethod.Delete, "A", new Type[0]);
			var differentRoute = new Route(HttpMethod.Put, "a", new Type[0]);
			route.Equals(differentRoute).Should().BeFalse("because the two routes have a different template and method");
		}

		[Test]
		public void TestTryMatch1([ValueSource(nameof(Types))] Type type)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { type });
			object[] values;
			route.TryMatch("", out values).Should().BeFalse();
			values.Should().BeNull();
		}

		#region Bool

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
		public void TestTryMatchBool3()
		{
			var route = new Route(HttpMethod.Get, @"{0}{1}", new[] { typeof(bool), typeof(bool) });
			object[] values;
			route.TryMatch(@"truefalse", out values).Should().BeTrue();
			values.Should().Equal(true, false);
			route.TryMatch(@"falsefalse", out values).Should().BeTrue();
			values.Should().Equal(false, false);
		}

		#endregion

		#region SByte

		[Test]
		public void TestConstructSByte1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] {typeof(sbyte), typeof(sbyte)}))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.SByte at index 0");
		}

		[Test]
		public void TestTryMatchSByte1([Values(sbyte.MinValue, -100, -99, -10, -9, 0, 9, 10, 99, 100, sbyte.MaxValue)] sbyte value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(sbyte) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}

		[Test]
		public void TestTryMatchSByte2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(sbyte) });
			object[] values;
			route.TryMatch("-129", out values).Should().BeFalse();
			route.TryMatch("128", out values).Should().BeFalse();
			route.TryMatch("-128/u", out values).Should().BeFalse();
			route.TryMatch("127&id=1", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchSByte3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(sbyte), typeof(sbyte) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal((sbyte)1, (sbyte)2);
		}

		#endregion

		#region Byte

		[Test]
		public void TestConstructByte1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] { typeof(byte), typeof(byte) }))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.Byte at index 0");
		}

		[Test]
		public void TestTryMatchByte1([Values(0, 9, 10, 99, 100, byte.MaxValue)] byte value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(byte) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}

		[Test]
		public void TestTryMatchByte2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(byte) });
			object[] values;
			route.TryMatch("-1", out values).Should().BeFalse();
			route.TryMatch("256", out values).Should().BeFalse();
			route.TryMatch("2550", out values).Should().BeFalse();
			route.TryMatch("256&id=1", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchByte3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(byte), typeof(byte) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal((byte)1, (byte)2);
		}

		#endregion

		#region Short

		[Test]
		public void TestConstructShort1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] { typeof(short), typeof(short) }))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.Int16 at index 0");
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
		public void TestTryMatchShort2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(short) });
			object[] values;
			route.TryMatch("-32769", out values).Should().BeFalse();
			route.TryMatch("32768", out values).Should().BeFalse();
			route.TryMatch("327670", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchShort3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(short), typeof(short) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal((short)1, (short)2);
		}

		#endregion

		#region UShort

		[Test]
		public void TestConstructUShort1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] { typeof(ushort), typeof(ushort) }))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.UInt16 at index 0");
		}

		[Test]
		public void TestTryMatchUShort1([Values(0, 9, 10, 99, 100, 999, 1000, 9999, 10000, ushort.MaxValue)] int value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(ushort) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal((ushort)value);
		}

		[Test]
		public void TestTryMatchUShort2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(ushort) });
			object[] values;
			route.TryMatch("-1", out values).Should().BeFalse();
			route.TryMatch("65536", out values).Should().BeFalse();
			route.TryMatch("655350", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchUShort3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(ushort), typeof(ushort) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal((ushort)1, (ushort)2);
		}

		#endregion

		#region Int

		[Test]
		public void TestConstructInt1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] { typeof(int), typeof(int) }))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.Int32 at index 0");
		}

		[Test]
		public void TestTryMatchInt1([Values(int.MinValue, -1000000000, -999999999, -100000000, -99999999, -10000000, -9999999, -100000, -99999, -10000, -9999, -1000, -999, -100, -99, -10, -9, 0, 9, 10, 99, 100, 999, 1000, 9999, 10000, 99999, 100000, 999999, 1000000, 9999999, 10000000, 99999999, 1000000000, int.MaxValue)] int value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(int) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}

		[Test]
		public void TestTryMatchInt2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(int) });
			object[] values;
			route.TryMatch("-2147483649", out values).Should().BeFalse();
			route.TryMatch("2147483648", out values).Should().BeFalse();
			route.TryMatch("21474836470", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchInt3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(int), typeof(int) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal(1, 2);
		}

		#endregion

		#region UInt

		[Test]
		public void TestConstructUInt1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] { typeof(uint), typeof(uint) }))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.UInt32 at index 0");
		}

		[Test]
		public void TestTryMatchUInt1([Values(0, 9, 10, 99, 100, 999, 1000, 9999, 10000, 99999, 100000, 999999, 1000000, 9999999, 10000000, 99999999, 1000000000, uint.MaxValue)] long value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(uint) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal((uint)value);
		}

		[Test]
		public void TestTryMatchUInt2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(uint) });
			object[] values;
			route.TryMatch("-1", out values).Should().BeFalse();
			route.TryMatch("4294967296", out values).Should().BeFalse();
			route.TryMatch("42949672950", out values).Should().BeFalse();
		}

		[Test]
		public void TestTryMatchUInt3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(uint), typeof(uint) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal((uint)1, (uint)2);
		}

		#endregion

		#region Long

		[Test]
		public void TestConstructLong1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] { typeof(long), typeof(long) }))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.Int64 at index 0");
		}

		[Test]
		public void TestTryMatchLong1([Values(long.MinValue, -1000000000, -999999999, -100000000, -99999999, -10000000, -9999999, -100000, -99999, -10000, -9999, -1000, -999, -100, -99, -10, -9, 0, 9, 10, 99, 100, 999, 1000, 9999, 10000, 99999, 100000, 999999, 1000000, 9999999, 10000000, 99999999, 1000000000, long.MaxValue)] long value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(long) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal(value);
		}

		[Test]
		public void TestTryMatchLong3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(long), typeof(long) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal((long)1, (long)2);
		}

		#endregion

		#region ULong

		[Test]
		public void TestConstructULong1()
		{
			new Action(() => new Route(HttpMethod.Get, "{0}{1}", new[] { typeof(ulong), typeof(ulong) }))
				.Should().Throw<ArgumentException>()
				.WithMessage("A terminator is required after argument of System.UInt64 at index 0");
		}

		[Test]
		public void TestTryMatchULong1([Values(0, 9, 10, 99, 100, 999, 1000, 9999, 10000, 99999, 100000, 999999, 1000000, 9999999, 10000000, 99999999, 1000000000)] long value)
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(ulong) });
			object[] values;
			route.TryMatch(value.ToString(), out values).Should().BeTrue();
			values.Should().Equal((ulong)value);
		}

		[Test]
		public void TestTryMatchULong2()
		{
			var route = new Route(HttpMethod.Get, "{0}", new[] { typeof(ulong) });
			object[] values;
			route.TryMatch(ulong.MaxValue.ToString(), out values).Should().BeTrue();
			values.Should().Equal(ulong.MaxValue);
		}

		[Test]
		public void TestTryMatchULong3()
		{
			var route = new Route(HttpMethod.Get, "{0}/{1}", new[] { typeof(ulong), typeof(ulong) });
			object[] values;
			route.TryMatch("1/2", out values).Should().BeTrue();
			values.Should().Equal((ulong)1, (ulong)2);
		}

		#endregion
	}
}