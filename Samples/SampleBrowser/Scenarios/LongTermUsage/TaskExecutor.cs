using System.Collections.Generic;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	public sealed class TaskExecutor
		: ITaskExecutor
	{
		private readonly List<ITaskController> _tasks;

		public TaskExecutor()
		{
			_tasks = new List<ITaskController>();
		}

		public ITaskController Create(int? numDataPackets)
		{
			var controller = new TaskController(numDataPackets);
			_tasks.Add(controller);
			return controller;
		}

		public void Remove(ITaskController controller)
		{
			_tasks.Remove(controller);
			((TaskController)controller).Dispose();
		}
	}
}