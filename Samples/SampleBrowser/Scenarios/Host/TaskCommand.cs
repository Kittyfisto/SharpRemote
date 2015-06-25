using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SampleBrowser.Scenarios.Host
{
	public class TaskCommand : ICommand
	{
		private readonly Func<Task> _runTaskCommand;
		private Task _task;

		public TaskCommand(Func<Task> runTaskCommand)
		{
			_runTaskCommand = runTaskCommand;
		}

		public bool CanExecute(object parameter)
		{
			return _task == null;
		}

		public void Execute(object parameter)
		{
			_task = _runTaskCommand();
			_task.ContinueWith(TaskFinished);
		}

		private void TaskFinished(Task task)
		{
			_task = null;
			// TODO: Execute on dispatcher thread?
			EmitCanExecuteChanged();
		}

		public event EventHandler CanExecuteChanged;

		private void EmitCanExecuteChanged()
		{
			EventHandler handler = CanExecuteChanged;
			if (handler != null) handler(this, EventArgs.Empty);
		}
	}
}