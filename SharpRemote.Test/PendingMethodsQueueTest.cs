using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class PendingMethodsQueueTest
	{
		[Test]
		public void TestDispose()
		{
			var queue = new PendingMethodsQueue();
			queue.IsDisposed.Should().BeFalse();
			queue.Dispose();
			queue.IsDisposed.Should().BeTrue();
			new Action(queue.Dispose).ShouldNotThrow();
			queue.IsDisposed.Should().BeTrue();
		}

		[Test]
		public void TestEnqueueTooMuch()
		{
			var queue = new PendingMethodsQueue(maxConcurrentCalls: 1)
				{
					IsConnected = true
				};

			Task.Factory.StartNew(() =>
				{
					queue.Enqueue(1, "", "", new MemoryStream(), 1);
				}).Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
			Task.Factory.StartNew(() =>
			{
				queue.Enqueue(1, "", "", new MemoryStream(), 2);
			}).Wait(TimeSpan.FromSeconds(1)).Should().BeFalse();
		}

		[Test]
		[Description("Verifies that enqueueing more items than the capacity doesn't deadlock when another thread consumes them again")]
		public void TestEnqueueDequeue()
		{
			var queue = new PendingMethodsQueue(maxConcurrentCalls: 1) {IsConnected = true};

			queue.Enqueue(1, "", "", new MemoryStream(), 1);
			var writeTask = Task.Factory.StartNew(() =>
			{
				queue.Enqueue(1, "", "", new MemoryStream(), 2);
			});
			writeTask.Wait(TimeSpan.FromMilliseconds(100)).Should().BeFalse();

			var messages = new List<byte[]>();
			var readTask = Task.Factory.StartNew(() =>
				{
					int length;
					messages.Add(queue.TakePendingWrite(out length));
					messages.Add(queue.TakePendingWrite(out length));
				});

			readTask.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
			writeTask.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
		}

		[Test]
		public void TestEnqueuePerformanceOneClient()
		{
			const int count = 1000000;
			var queue = new PendingMethodsQueue(maxConcurrentCalls: count) {IsConnected = true};

			// Warm-up...
			var stream = new MemoryStream();
			for (int i = 0; i < 100000; ++i)
			{
				var call = queue.Enqueue(1, "", "", stream, i);
				int unused;
				queue.TakePendingWrite(out unused);
				queue.Recycle(call);
			}

			var watch = new Stopwatch();
			watch.Start();
			for (int i = 0; i < count; ++i)
			{
				queue.Enqueue(1, "", "", stream, i);
			}
			watch.Stop();

			var numSeconds = watch.Elapsed.TotalSeconds;
			var ops = 1.0 * count / numSeconds;
			Console.WriteLine("Total calls: {0}", count);
			Console.WriteLine("OP/s: {0:F2}k/s", ops / 1000);
			Console.WriteLine("Latency: {0}ns", watch.ElapsedTicks*100/count);
		}

		[Test]
		public void TestDequeueDisconnected()
		{
			const int capacity = 2;
			using (var queue = new PendingMethodsQueue(maxConcurrentCalls: capacity))
			{
				int length;
				new Action(() => queue.TakePendingWrite(out length))
					.ShouldThrow<OperationCanceledException>();
			}
		}

		[Test]
		[Description("Verifies that after all pending calls have been cancelled, the queue is empty AND stays empty")]
		public void TestCancelPendingCalls()
		{
			const int capacity = 2;
			using (var queue = new PendingMethodsQueue(maxConcurrentCalls: capacity))
			{
				queue.IsConnected = true;

				var task1 = Task.Factory.StartNew(() =>
					{
						queue.Enqueue(42, "foo", "bar", new MemoryStream(), 0);
					});
				var task2 = Task.Factory.StartNew(() =>
					{
						queue.Enqueue(42, "foo", "bar", new MemoryStream(), 1);
					});

				task1.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
				task2.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();

				var task3 = Task.Factory.StartNew(() =>
					{
						
						queue.Enqueue(42, "foo", "bar", new MemoryStream(), 2);
					});

				// Waiting on task3 would produce a deadlock because it's waiting for the queue to make room
				queue.NumPendingCalls.Should().Be(2);
				queue.CancelAllCalls();
				queue.NumPendingCalls.Should().Be(0);

				try
				{
					task3.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
				}
				catch (AggregateException e)
				{
					e.InnerException.Should().BeOfType<OperationCanceledException>();
				}

				queue.NumPendingCalls.Should().Be(0);
			}
		}
	}
}