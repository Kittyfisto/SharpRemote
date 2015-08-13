using System.Diagnostics;
using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class OutOfProcessSiloServerTest
	{
		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			
		}

		[Test]
		public void TestCtor1()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
					"true", //< collect minidumps
					"false", //< suppress error windows
					"false", //< handle access violations
					"false", //< handle crt asserts
					"false", //< handle pure virtual function calls
					((int)(CRuntimeVersions._110 | CRuntimeVersions.Release)).ToString(CultureInfo.InvariantCulture),
					"100", //< num minidumps retained,
					@"C:\foo dumps\",
					"Test"
				};
			using (var server = new OutOfProcessSiloServer(args))
			{
				var actualSettings = server.PostMortemSettings;
				actualSettings.CollectMinidumps.Should().BeTrue();
				actualSettings.SuppressErrorWindows.Should().BeFalse();
				actualSettings.HandleAccessViolations.Should().BeFalse();
				actualSettings.HandleCrtAsserts.Should().BeFalse();
				actualSettings.HandleCrtPureVirtualFunctionCalls.Should().BeFalse();
				actualSettings.RuntimeVersions.Should().Be(CRuntimeVersions._110 | CRuntimeVersions.Release);
				actualSettings.NumMinidumpsRetained.Should().Be(100);
				actualSettings.MinidumpFolder.Should().Be(@"C:\foo dumps\");
				actualSettings.MinidumpName.Should().Be("Test");
			}
		}

		[Test]
		public void TestCtor2()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
					"false", //< collect minidumps
					"true", //< suppress error windows
					"false", //< handle access violations
					"false", //< handle crt asserts
					"false", //< handle pure virtual function calls
					((int)(CRuntimeVersions._71 | CRuntimeVersions.Debug)).ToString(CultureInfo.InvariantCulture),
					"100", //< num minidumps retained,
					@"C:\foo dumps\",
					"Test"
				};
			using (var server = new OutOfProcessSiloServer(args))
			{
				var actualSettings = server.PostMortemSettings;
				actualSettings.CollectMinidumps.Should().BeFalse();
				actualSettings.SuppressErrorWindows.Should().BeTrue();
				actualSettings.HandleAccessViolations.Should().BeFalse();
				actualSettings.HandleCrtAsserts.Should().BeFalse();
				actualSettings.HandleCrtPureVirtualFunctionCalls.Should().BeFalse();
				actualSettings.RuntimeVersions.Should().Be(CRuntimeVersions._71 | CRuntimeVersions.Debug);
				actualSettings.NumMinidumpsRetained.Should().Be(100);
				actualSettings.MinidumpFolder.Should().Be(@"C:\foo dumps\");
				actualSettings.MinidumpName.Should().Be("Test");
			}
		}

		[Test]
		public void TestCtor3()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
					"false", //< collect minidumps
					"false", //< suppress error windows
					"true", //< handle access violations
					"false", //< handle crt asserts
					"false", //< handle pure virtual function calls
					((int)(CRuntimeVersions._80 | CRuntimeVersions.Release)).ToString(CultureInfo.InvariantCulture),
					"100", //< num minidumps retained,
					@"C:\foo dumps\",
					"Test"
				};
			using (var server = new OutOfProcessSiloServer(args))
			{
				var actualSettings = server.PostMortemSettings;
				actualSettings.CollectMinidumps.Should().BeFalse();
				actualSettings.SuppressErrorWindows.Should().BeFalse();
				actualSettings.HandleAccessViolations.Should().BeTrue();
				actualSettings.HandleCrtAsserts.Should().BeFalse();
				actualSettings.HandleCrtPureVirtualFunctionCalls.Should().BeFalse();
				actualSettings.RuntimeVersions.Should().Be(CRuntimeVersions._80 | CRuntimeVersions.Release);
				actualSettings.NumMinidumpsRetained.Should().Be(100);
				actualSettings.MinidumpFolder.Should().Be(@"C:\foo dumps\");
				actualSettings.MinidumpName.Should().Be("Test");
			}
		}

		[Test]
		public void TestCtor4()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
					"false", //< collect minidumps
					"false", //< suppress error windows
					"false", //< handle access violations
					"true", //< handle crt asserts
					"false", //< handle pure virtual function calls
					((int)(CRuntimeVersions._90 | CRuntimeVersions.Debug)).ToString(CultureInfo.InvariantCulture),
					"100", //< num minidumps retained,
					@"C:\foo dumps\",
					"Test"
				};
			using (var server = new OutOfProcessSiloServer(args))
			{
				var actualSettings = server.PostMortemSettings;
				actualSettings.CollectMinidumps.Should().BeFalse();
				actualSettings.SuppressErrorWindows.Should().BeFalse();
				actualSettings.HandleAccessViolations.Should().BeFalse();
				actualSettings.HandleCrtAsserts.Should().BeTrue();
				actualSettings.HandleCrtPureVirtualFunctionCalls.Should().BeFalse();
				actualSettings.RuntimeVersions.Should().Be(CRuntimeVersions._90 | CRuntimeVersions.Debug);
				actualSettings.NumMinidumpsRetained.Should().Be(100);
				actualSettings.MinidumpFolder.Should().Be(@"C:\foo dumps\");
				actualSettings.MinidumpName.Should().Be("Test");
			}
		}

		[Test]
		public void TestCtor5()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
					"false", //< collect minidumps
					"false", //< suppress error windows
					"false", //< handle access violations
					"false", //< handle crt asserts
					"true", //< handle pure virtual function calls
					((int)(CRuntimeVersions._100 | CRuntimeVersions.Release)).ToString(CultureInfo.InvariantCulture),
					"100", //< num minidumps retained,
					@"C:\foo dumps\",
					"Test"
				};
			using (var server = new OutOfProcessSiloServer(args))
			{
				var actualSettings = server.PostMortemSettings;
				actualSettings.CollectMinidumps.Should().BeFalse();
				actualSettings.SuppressErrorWindows.Should().BeFalse();
				actualSettings.HandleAccessViolations.Should().BeFalse();
				actualSettings.HandleCrtAsserts.Should().BeFalse();
				actualSettings.HandleCrtPureVirtualFunctionCalls.Should().BeTrue();
				actualSettings.RuntimeVersions.Should().Be(CRuntimeVersions._100 | CRuntimeVersions.Release);
				actualSettings.NumMinidumpsRetained.Should().Be(100);
				actualSettings.MinidumpFolder.Should().Be(@"C:\foo dumps\");
				actualSettings.MinidumpName.Should().Be("Test");
			}
		}

		[Test]
		public void TestCtor6()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
					"true", //< collect minidumps
					"true", //< suppress error windows
					"true", //< handle access violations
					"true", //< handle crt asserts
					"true", //< handle pure virtual function calls
					((int)(CRuntimeVersions.AllRelease)).ToString(CultureInfo.InvariantCulture),
					"100", //< num minidumps retained,
					@"C:\foo dumps\",
					"Test"
				};
			using (var server = new OutOfProcessSiloServer(args))
			{
				var actualSettings = server.PostMortemSettings;
				actualSettings.CollectMinidumps.Should().BeTrue();
				actualSettings.SuppressErrorWindows.Should().BeTrue();
				actualSettings.HandleAccessViolations.Should().BeTrue();
				actualSettings.HandleCrtAsserts.Should().BeTrue();
				actualSettings.HandleCrtPureVirtualFunctionCalls.Should().BeTrue();
				actualSettings.RuntimeVersions.Should().Be(CRuntimeVersions.AllRelease);
				actualSettings.NumMinidumpsRetained.Should().Be(100);
				actualSettings.MinidumpFolder.Should().Be(@"C:\foo dumps\");
				actualSettings.MinidumpName.Should().Be("Test");
			}
		}
	}
}