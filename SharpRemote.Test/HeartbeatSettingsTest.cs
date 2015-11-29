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
			@default.UseHeartbeatFailureDetection.Should().BeTrue();
			@default.SkippedHeartbeatThreshold.Should().Be(10);
		}
	}
}