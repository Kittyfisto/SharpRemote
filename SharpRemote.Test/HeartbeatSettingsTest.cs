using System;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class HeartbeatSettingsTest
	{
		[Test]
		[Description("Verifies that default values aren't changed")]
		public void TestCtor()
		{
			var @default = new HeartbeatSettings();
			@default.Interval.Should().Be(TimeSpan.FromSeconds(1));
			@default.ReportSkippedHeartbeatsAsFailureWithDebuggerAttached.Should().BeFalse();
			@default.AllowRemoteHeartbeatDisable.Should().BeFalse();
			@default.UseHeartbeatFailureDetection.Should().BeTrue();
			@default.SkippedHeartbeatThreshold.Should().Be(10);
			@default.ReportDebuggerAttached.Should().BeTrue();
		}

		[Test]
		public void TestConstants()
		{
			var settings = HeartbeatSettings.Dont;
			settings.UseHeartbeatFailureDetection.Should().BeFalse("because heartbeat measurements should be disabled");

			settings.UseHeartbeatFailureDetection = true;
			HeartbeatSettings.Dont.UseHeartbeatFailureDetection.Should().BeFalse("because the constant itself should not have been modified");
		}
	}
}