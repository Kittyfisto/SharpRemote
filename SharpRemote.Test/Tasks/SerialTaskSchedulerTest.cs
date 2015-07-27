using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Tasks;

namespace SharpRemote.Test.Tasks
{
	[TestFixture]
	public sealed class SerialTaskSchedulerTest
	{
		[Test]
		public void TestCtor()
		{
			var scheduler = new SerialTaskScheduler(true);
			scheduler.Dispose();
			scheduler.Exceptions.Should().BeEmpty();
		}

		[Test]
		public void TestScheduleManyTasks()
		{
			var scheduler = new SerialTaskScheduler(true);
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
			var scheduler = new SerialTaskScheduler(true);
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
			using (var scheduler = new SerialTaskScheduler(true))
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
	}
}