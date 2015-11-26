using System;
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
		[LocalTest("Doesn't work on build server")]
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

		[Test]
		[Description("Verifies that the settings passed to the ctor are properly forwarded to the socket endpoint")]
		public void TestCtor7()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
				};
			var heartbeatSettings = new HeartbeatSettings
				{
					Interval = TimeSpan.FromSeconds(1.5),
					ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
					SkippedHeartbeatThreshold = 11
				};
			var latencySettings = new LatencySettings
				{
					Interval = TimeSpan.FromSeconds(1.5),
					NumSamples = 8,
					PerformLatencyMeasurements = true
				};

			using (var server = new OutOfProcessSiloServer(args,
				heartbeatSettings: heartbeatSettings,
				latencySettings: latencySettings))
			{
				var endPoint = server.EndPoint;
				endPoint.Should().NotBeNull();

				endPoint.LatencySettings.Should().BeSameAs(latencySettings);
				endPoint.LatencySettings.Interval.Should().Be(TimeSpan.FromSeconds(1.5));
				endPoint.LatencySettings.NumSamples.Should().Be(8);
				endPoint.LatencySettings.PerformLatencyMeasurements.Should().BeTrue();

				endPoint.HeartbeatSettings.Should().BeSameAs(heartbeatSettings);
				endPoint.HeartbeatSettings.Interval.Should().Be(TimeSpan.FromSeconds(1.5));
				endPoint.HeartbeatSettings.ReportSkippedHeartbeatsAsFailureWithDebuggerAttached.Should().BeTrue();
				endPoint.HeartbeatSettings.SkippedHeartbeatThreshold.Should().Be(11);
			}
		}
	}
}