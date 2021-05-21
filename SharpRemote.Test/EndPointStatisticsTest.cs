using System;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace SharpRemote.Test
{
	/// <summary>
	/// 
	/// </summary>
	[TestFixture]
	public sealed class EndPointStatisticsTest
	{
		private Mock<IRemotingEndPoint> _endPoint;

		[SetUp]
		public void Setup()
		{
			_endPoint = new Mock<IRemotingEndPoint>();
		}

		[Test]
		public void TestDispose1()
		{
			var statistics = new EndPointStatistics(_endPoint.Object);
			new Action(() => statistics.Dispose()).Should().NotThrow();
		}

		[Test]
		public void TestDispose2()
		{
			var statistics = new EndPointStatistics(_endPoint.Object);
			statistics.Start();
			new Action(() => statistics.Dispose()).Should().NotThrow();
		}

		[Test]
		[SetCulture("en-US")]
		public void TestLogBytesReceived()
		{
			var statistics = new EndPointStatistics(_endPoint.Object);

			_endPoint.Setup(x => x.NumBytesReceived).Returns(1024);
			statistics.Update();
			statistics.CreateReport().Should().Contain("1.0 Kb/s");

			_endPoint.Setup(x => x.NumBytesReceived).Returns(1400);
			statistics.Update();
			statistics.CreateReport().Should().Contain("0.7 Kb/s");

			_endPoint.Setup(x => x.NumBytesReceived).Returns(42000);
			statistics.Update();
			statistics.CreateReport().Should().Contain("13.7 Kb/s");

			_endPoint.Setup(x => x.NumBytesReceived).Returns(42000000);
			statistics.Update();
			statistics.CreateReport().Should().Contain("10253.9 Kb/s");
		}

		[Test]
		[SetCulture("en-US")]
		public void TestLogBytesSent()
		{
			var statistics = new EndPointStatistics(_endPoint.Object);

			_endPoint.Setup(x => x.NumBytesSent).Returns(1024);
			statistics.Update();
			statistics.CreateReport().Should().Contain("1.0 Kb/s");

			_endPoint.Setup(x => x.NumBytesSent).Returns(1400);
			statistics.Update();
			statistics.CreateReport().Should().Contain("0.7 Kb/s");

			_endPoint.Setup(x => x.NumBytesSent).Returns(42000);
			statistics.Update();
			statistics.CreateReport().Should().Contain("13.7 Kb/s");

			_endPoint.Setup(x => x.NumBytesSent).Returns(42000000);
			statistics.Update();
			statistics.CreateReport().Should().Contain("10253.9 Kb/s");
		}

		[Test]
		[SetCulture("en-US")]
		public void TestLogGcTime()
		{
			var statistics = new EndPointStatistics(_endPoint.Object);
			_endPoint.Setup(x => x.TotalGarbageCollectionTime).Returns(TimeSpan.Zero);
			statistics.Update();
			statistics.CreateReport().Should().Contain("avg. GC: 0.00%");

			_endPoint.Setup(x => x.TotalGarbageCollectionTime).Returns(TimeSpan.FromSeconds(1));
			statistics.Update();
			statistics.CreateReport().Should().Contain("avg. GC: 50.00%");

			statistics.Update();
			statistics.CreateReport().Should().Contain("avg. GC: 33.33%");
		}

		[Test]
		public void TestLogNumPendingMethodCalls([Values(0, 42, 1000)] int numPendingMethodCalls)
		{
			var statistics = new EndPointStatistics(_endPoint.Object);

			_endPoint.Setup(x => x.NumPendingMethodCalls).Returns(numPendingMethodCalls);
			statistics.Update();

			statistics.CreateReport().Should().Contain(string.Format("Pending method calls: {0}", numPendingMethodCalls));
		}

		[Test]
		public void TestLogNumPendingMethodInvocations([Values(0, 42, 1000)] int numPendingMethodInvocations)
		{
			var statistics = new EndPointStatistics(_endPoint.Object);

			_endPoint.Setup(x => x.NumPendingMethodInvocations).Returns(numPendingMethodInvocations);
			statistics.Update();

			statistics.CreateReport().Should().Contain(string.Format("Pending method invocations: {0}", numPendingMethodInvocations));
		}

		[Test]
		[SetCulture("en-US")]
		public void TestLogNumServantsCollected()
		{
			var statistics = new EndPointStatistics(_endPoint.Object);

			_endPoint.Setup(x => x.NumServantsCollected).Returns(1);
			statistics.Update();
			statistics.CreateReport().Should().Contain("Servants collected: 1.0/s");

			statistics.Update();
			statistics.CreateReport().Should().Contain("Servants collected: 0.5/s");

			_endPoint.Setup(x => x.NumServantsCollected).Returns(3);
			statistics.Update();
			statistics.CreateReport().Should().Contain("Servants collected: 1.0/s");
		}

		[Test]
		[SetCulture("en-US")]
		public void TestLogNumProxiesCollected()
		{
			var statistics = new EndPointStatistics(_endPoint.Object);

			_endPoint.Setup(x => x.NumProxiesCollected).Returns(1);
			statistics.Update();
			statistics.CreateReport().Should().Contain("Proxies collected: 1.0/s");

			statistics.Update();
			statistics.CreateReport().Should().Contain("Proxies collected: 0.5/s");

			_endPoint.Setup(x => x.NumProxiesCollected).Returns(3);
			statistics.Update();
			statistics.CreateReport().Should().Contain("Proxies collected: 1.0/s");
		}
	}
}