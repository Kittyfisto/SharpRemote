using System;
using System.Diagnostics;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class LatencyMonitorTest
	{
		private Mock<ILatency> _latency;

		[SetUp]
		public void TestSetUp()
		{
			_latency = new Mock<ILatency>();
		}

		[Test]
		[Description("Verifies that Start() sets the IsStarted property to true")]
		public void TestStart()
		{
			using (var monitor = new LatencyMonitor(_latency.Object, new LatencySettings()))
			{
				monitor.IsStarted.Should().BeFalse();
				monitor.Start();
				monitor.IsStarted.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Stop() sets the IsStarted property to false")]
		public void TestStop()
		{
			using (var monitor = new LatencyMonitor(_latency.Object, new LatencySettings()))
			{
				monitor.Start();
				monitor.IsStarted.Should().BeTrue();
				monitor.Stop();
				monitor.IsStarted.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that Dispose sets the IsStarted property to false, even when Stop() hasn't been called")]
		public void TestDispose()
		{
			LatencyMonitor monitor;
			using (monitor = new LatencyMonitor(_latency.Object, new LatencySettings()))
			{
				monitor.IsDisposed.Should().BeFalse();

				monitor.Start();
				monitor.IsStarted.Should().BeTrue();
			}
			monitor.IsStarted.Should().BeFalse();
			monitor.IsDisposed.Should().BeTrue();
		}

		[Test]
		public void TestMeasureOnce()
		{
			using (var monitor = new LatencyMonitor(_latency.Object, new LatencySettings()))
			{
				_latency.Verify(x => x.Roundtrip(), Times.Never, "because Roundtrip() shouldn't have been called just yet");

				TimeSpan unused;
				monitor.MeasureLatency(new Stopwatch(), out unused);
				_latency.Verify(x => x.Roundtrip(), Times.Once, "because Roundtrip() should've been invoked exactly once during MeasureLatency()");
			}
		}
	}
}