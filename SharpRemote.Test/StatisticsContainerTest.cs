using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class StatisticsContainerTest
	{
		[Test]
		public void TestNoSample()
		{
			var stats = new StatisticsContainer(42);
			stats.Average.Should().Be(0);
		}

		[Test]
		public void TestNoSample([Values(0, 1, 42, -1)] long value)
		{
			var stats = new StatisticsContainer(42);
			stats.Enqueue(value);
			stats.Average.Should().Be(value);
		}

		[Test]
		public void TestTwoSamples()
		{
			var stats = new StatisticsContainer(10);
			stats.Enqueue(1);
			stats.Average.Should().Be(1);

			stats.Enqueue(2);
			stats.Average.Should().Be(1.5);

			stats.Enqueue(6);
			stats.Average.Should().Be(3);
		}

		[Test]
		public void TestSomeSamples()
		{
			var stats = new StatisticsContainer(2);
			stats.Enqueue(1);
			stats.Average.Should().Be(1);

			stats.Enqueue(2);
			stats.Average.Should().Be(1.5);

			stats.Enqueue(2);
			stats.Average.Should().Be(2);
			
			stats.Enqueue(6);
			stats.Average.Should().Be(4);
		}

		[Test]
		public void TestManySamples()
		{
			var stats = new StatisticsContainer(10);

			for (int i = 0; i < 100; ++i)
			{
				stats.Enqueue(42);
				stats.Average.Should().Be(42);
			}
		}
	}
}