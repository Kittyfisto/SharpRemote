using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
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

			var source = new CancellationTokenSource();
			var messages = new List<byte[]>();
			var readTask = Task.Factory.StartNew(() =>
				{
					int length;
					messages.Add(queue.TakePendingWrite(source.Token, out length));
					messages.Add(queue.TakePendingWrite(source.Token, out length));
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
			var source = new CancellationTokenSource();
			for (int i = 0; i < 100000; ++i)
			{
				var call = queue.Enqueue(1, "", "", stream, i);
				int unused;
				queue.TakePendingWrite(source.Token, out unused);
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
	}
}