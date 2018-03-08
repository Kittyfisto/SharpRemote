using System;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class LatencySettingsTest
	{
		[Test]
		public void TestCtor()
		{
			var settings = new LatencySettings();
			settings.PerformLatencyMeasurements.Should().BeTrue();
			settings.NumSamples.Should().Be(10);
			settings.Interval.Should().Be(TimeSpan.FromMilliseconds(100));
		}

		[Test]
		[Description("Verifies that the 'DontMeasure' singleton cannot be changed")]
		public void TestConstants()
		{
			var settings = LatencySettings.DontMeasure;
			settings.PerformLatencyMeasurements.Should().BeFalse("because latency measurements should be disabled");

			settings.PerformLatencyMeasurements = true;
			LatencySettings.DontMeasure.PerformLatencyMeasurements.Should()
			               .BeFalse("because the constant itself should not have been modified");
		}
	}
}