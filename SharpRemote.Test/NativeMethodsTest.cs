using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Extensions;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class NativeMethodsTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var dir = Assembly.GetExecutingAssembly().GetDirectory();
			dir = Path.Combine(dir, Environment.Is64BitProcess ? "x64" : "x86");

			NativeMethods.SetDllDirectory(dir);
		}

		[Test]
		[Description("Verifies that specifying 0 as the amount of retained dumps is not allowed")]
		public void TestInit()
		{
			NativeMethods.Init(0, @"C:\dumps\", "test").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a negative number as the amount of retained dumps is not allowed")]
		public void TestInit2()
		{
			NativeMethods.Init(-1, @"C:\dumps\", "test").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a null folder is not allowed")]
		public void TestInit3()
		{
			NativeMethods.Init(10, null, "test").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a non-rooted folder is not allowed")]
		public void TestInit4()
		{
			NativeMethods.Init(10, @"temp\", "test").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a folder with '/' is not allowed")]
		public void TestInit5()
		{
			NativeMethods.Init(10, @"C:/dumps\", "test").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a folder that doesn't end in '\' is not allowed")]
		public void TestInit6()
		{
			NativeMethods.Init(10, @"C:\dumps", "test").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a null dump name is not allowed")]
		public void TestInit7()
		{
			NativeMethods.Init(10, @"C:\dumps\", null).Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying an empty dump name is not allowed")]
		public void TestInit8()
		{
			NativeMethods.Init(10, @"C:\dumps\", "").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a dump name containing '/' is not allowed")]
		public void TestInit9()
		{
			NativeMethods.Init(10, @"C:\dumps\", "foo/bar").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a dump name containing '\\' is not allowed")]
		public void TestInit10()
		{
			NativeMethods.Init(10, @"C:\dumps\", "foo\\bar").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a dump name containing '..' is not allowed")]
		public void TestInit11()
		{
			NativeMethods.Init(10, @"C:\dumps\", "foo..bar").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a dump name containing '?' is not allowed")]
		public void TestInit12()
		{
			NativeMethods.Init(10, @"C:\dumps\", "foo?bar").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a dump name containing '\"' is not allowed")]
		public void TestInit13()
		{
			NativeMethods.Init(10, @"C:\dumps\", "foo\\bar").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}

		[Test]
		[Description("Verifies that specifying a dump name containing '?' is not allowed")]
		public void TestInit14()
		{
			NativeMethods.Init(10, @"C:\dumps\", "foo?bar").Should().BeFalse();
			Marshal.GetLastWin32Error().Should().Be(160);
		}
	}
}