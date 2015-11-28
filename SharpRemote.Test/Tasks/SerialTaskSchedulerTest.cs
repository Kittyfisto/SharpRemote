using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Tasks;
using SharpRemote.Test.Remoting.SocketRemotingEndPoint;

namespace SharpRemote.Test.Tasks
{
	[TestFixture]
	public sealed class SerialTaskSchedulerTest
		: AbstractTest
	{
		[Test]
		public void TestCtor()
		{
			var scheduler = new SerialTaskScheduler(logExceptions: true);
			scheduler.Dispose();
			scheduler.Exceptions.Should().BeEmpty();
		}

		[Test]
		public void TestScheduleManyTasks()
		{
			var scheduler = new SerialTaskScheduler(logExceptions: true);
			const int taskCount = 1000;
			var tasks = new List<Task<int>>(taskCount);
			for (int i = 0; i < taskCount; ++i)
			{
				var task = scheduler.QueueTask(() => 42);
				tasks.Add(task);
			}
			Task.WaitAll(tasks.Cast<Task>().ToArray(), TimeSpan.FromSeconds(10))
			    .Should().BeTrue();
			tasks.All(x => x.Result == 42).Should().BeTrue();
			scheduler.Exceptions.Should().BeEmpty();
		}

		[Test]
		public void TestScheduleOneTask()
		{
			var scheduler = new SerialTaskScheduler(logExceptions: true);
			var task = scheduler.QueueTask(() => 42);

			task.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
			task.Result.Should().Be(42);
			scheduler.Dispose();
			scheduler.Exceptions.Should().BeEmpty();
		}

		[Test]
		[Description("Ensures that tasks executed within a task, scheduled by a serial task scheduler, do not high-jack the serial task scheduler for execution, but the default one, even without having been specified")]
		public void TestSchedulerHighjacking()
		{
			using (var scheduler = new SerialTaskScheduler(logExceptions: true))
			{
				TaskScheduler actualInnerScheduler = null;
				var task = scheduler.QueueTask(() =>
					{
						var task2 = new Task<int>(() =>
							{
								actualInnerScheduler = TaskScheduler.Current;
								return 42;
							});
						task2.Start();
						return task2.Result;
					});

				task.Wait(TimeSpan.FromSeconds(1)).Should().BeTrue();
				task.Result.Should().Be(42);
				actualInnerScheduler.Should().BeSameAs(TaskScheduler.Default);
			}
		}

		[Test]
		[Description("Verifies that the executing thread is only started once a task has been queued and that it is stopped if no task has been queued for at least the specified amount timr")]
		public void TestStarStopThread()
		{
			using (var scheduler = new SerialTaskScheduler(TimeSpan.FromMilliseconds(100), true))
			{
				scheduler.IsExecutingThreadRunning.Should().BeFalse("Because no task has ever been scheduled and thus no additional thread should be waiting for it");
				scheduler.QueueTask(() => { });
				scheduler.IsExecutingThreadRunning.Should().BeTrue("Because a task has just been queued");

				// Fucking AppVeyor is so incredibly slow, no amount of timeout is enough.
				WaitFor(() => scheduler.IsExecutingThreadRunning == false,
				        TimeSpan.FromSeconds(10)).Should().BeTrue("Because for twice the timeout no additional task has been executed and thus the thread should've been ended");

				scheduler.Exceptions.Should().BeEmpty("Because no exceptions should've been thrown in the process");
			}
		}
	}
}