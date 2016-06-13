using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class BlockingCollectionTest
	{
		[Test]
		public void TestEnqueueOne()
		{
			using (var queue = new BlockingQueue<int>(1))
			{
				queue.Count.Should().Be(0);
				Task.Factory.StartNew(() => queue.Enqueue(42))
				    .Wait(500)
				    .Should().BeTrue();
				queue.Count.Should().Be(1);
			}
		}

		[Test]
		public void TestEnqueueDequeueOne()
		{
			using (var queue = new BlockingQueue<int>(1))

			{
				Task.Factory.StartNew(() => queue.Enqueue(42)).Wait(500).Should().BeTrue();
				int value = 0;
				Task.Factory.StartNew(() => value = queue.Dequeue()).Wait(500).Should().BeTrue();
				value.Should().Be(42);

				queue.Count.Should().Be(0);
			}
		}

		[Test]
		public void TestEnqueueBeyondCapacity1()
		{
			Task task;
			using (var queue = new BlockingQueue<int>(1))
			{
				Task.Factory.StartNew(() => queue.Enqueue(42)).Wait(500).Should().BeTrue();
				task = Task.Factory.StartNew(() => queue.Enqueue(9001));
				task.Wait(100).Should().BeFalse();
			}
			new Action(() => task.Wait(500)).ShouldThrow<OperationCanceledException>();
		}

		[Test]
		public void TestEnqueueBeyondCapacity2()
		{
			Task task;
			using (var queue = new BlockingQueue<int>(1))
			{
				Task.Factory.StartNew(() => queue.Enqueue(42)).Wait(500).Should().BeTrue();
				task = Task.Factory.StartNew(() => queue.Enqueue(9001));

				var item1 = Task.Factory.StartNew(() => queue.Dequeue());
				item1.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue("Because dequeuing an item should succeed within 1 second");

				var item2 = Task.Factory.StartNew(() => queue.Dequeue());

				task.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue("Because enqueueing an item should succeed within 1 second");
				item2.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue("Because the queue has an item again and thus dequeueing should succeed again");

				item1.Result.Should().Be(42);
				item2.Result.Should().Be(9001);
			}
		}

		[Test]
		public void TestEnqueueDequeueMany()
		{
			using (var queue = new BlockingQueue<int>(10))
			{
				const int count = 10000;
				var source = Enumerable.Range(0, count).ToArray();
				var dest = new List<int>(count);

				var writer = Task.Factory.StartNew(() =>
					{
						foreach (var value in source)
						{
							queue.Enqueue(value);
						}
					});
				var reader = Task.Factory.StartNew(() =>
					{
						for (int i = 0; i < count; ++i)
						{
							dest.Add(queue.Dequeue());
						}
					});

				writer.Wait(500).Should().BeTrue();
				reader.Wait(500).Should().BeTrue();

				dest.Should().Equal(source);
			}
		}

		[Test]
		public void TestManyWritersOneReader()
		{
			using (var queue = new BlockingQueue<int>(10))
			{
				const int numWriters = 10;
				const int numValues = 10000;

				var source = Enumerable.Range(0, numValues).ToArray();
				var dest = new List<int>(numWriters*numValues);
				var writers = Enumerable.Range(0, numWriters).Select(i => Task.Factory.StartNew(() =>
					{
						foreach (var value in source)
						{
							queue.Enqueue(value);
						}
					}, TaskCreationOptions.LongRunning)).ToArray();
				var reader = Task.Factory.StartNew(() =>
					{
						for (int i = 0; i < numWriters*numValues; ++i)
						{
							dest.Add(queue.Dequeue());
						}
					}, TaskCreationOptions.LongRunning);

				Task.WaitAll(writers, TimeSpan.FromSeconds(5)).Should().BeTrue();
				reader.Wait(500).Should().BeTrue();

				dest.Count.Should().Be(numWriters*numValues);

				foreach (var value in source)
				{
					dest.Count(x => x == value).Should().Be(numWriters);
				}
			}
		}

		[Test]
		public void TestDequeueWhileEmpty()
		{
			Task task;
			using (var queue = new BlockingQueue<int>(1))
			{
				task = Task.Factory.StartNew(() => queue.Dequeue());
				task.Wait(100).Should().BeFalse();
			}
			new Action(() => task.Wait(500))
				.ShouldThrow<OperationCanceledException>();
		}
	}
}