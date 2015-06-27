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
			scheduler.MaximumConcurrencyLevel.Should().Be(1);
			scheduler.Dispose();
			scheduler.Exceptions.Should().BeEmpty();
		}

		[Test]
		public void TestScheduleOneTask()
		{
			var scheduler = new SerialTaskScheduler(true);
			var task = new Task<int>(() => 42);
			task.Start(scheduler);
			task.Wait(TimeSpan.FromSeconds(10)).Should().BeTrue();
			task.Result.Should().Be(42);
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
				var task = new Task<int>(() => 42);
				tasks.Add(task);
				task.Start(scheduler);
			}
			Task.WaitAll(tasks.Cast<Task>().ToArray(), TimeSpan.FromSeconds(10))
			    .Should().BeTrue();
			tasks.All(x => x.Result == 42).Should().BeTrue();
			scheduler.Exceptions.Should().BeEmpty();
		}
	}
}