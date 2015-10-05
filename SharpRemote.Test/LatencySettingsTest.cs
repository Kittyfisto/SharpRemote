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
	}
}