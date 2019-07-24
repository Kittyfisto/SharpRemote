using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	public sealed class OutOfProcessSiloServerTest
	{
		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			
		}

		[Test]
		[Description("Verifies that EncodeException() encodes the given exception in a string as base64")]
		public void TestEncodeException()
		{
			var exception = new ArgumentNullException("whatever");
			string encoded = null;
			new Action(() => encoded = OutOfProcessSiloServer.EncodeException(exception))
				.ShouldNotThrow();

			encoded.Should().NotBeNull();
			encoded.Length.Should().BeGreaterThan(0);

			using (var stream = new MemoryStream(Convert.FromBase64String(encoded)))
			using (var reader = new BinaryReader(stream))
			{
				var actualException = AbstractEndPoint.ReadException(reader);
				actualException.Should().NotBeNull();
				actualException.Should().BeOfType<ArgumentNullException>();
				actualException.Message.Should().Be(exception.Message);
			}
		}

		[Test]
		[Description("Verifies that the settings passed to the ctor are properly forwarded to the socket endpoint")]
		public void TestCtor()
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

		[Test]
		[Description("Verifies that it's possible to specify the name of the silo")]
		public void TestCtor8()
		{
			var args = new[]
				{
					Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture),
				};
			using (var server = new OutOfProcessSiloServer(args,
				endPointName: "Foobar"))
			{
				server.Name.Should().Be("Foobar", "because we specified this name in the ctor");
			}
		}
	}
}