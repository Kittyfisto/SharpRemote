using System;
using System.Windows.Input;

namespace SampleBrowser.Scenarios
{
	public class DelegateCommand
		: ICommand
	{
		private readonly Action<object> _execute;
		private bool _canBeExecuted;

		public DelegateCommand(Action<object> execute)
		{
			_execute = execute;
		}

		public bool CanExecute(object parameter)
		{
			return CanBeExecuted;
		}

		public bool CanBeExecuted
		{
			get { return _canBeExecuted; }
			set
			{
				if (value == _canBeExecuted)
					return;

				_canBeExecuted = value;
				RaiseCanExecuteChanged();
			}
		}

		public void Execute(object parameter)
		{
			_execute(parameter);
		}

		public event EventHandler CanExecuteChanged;

		public void RaiseCanExecuteChanged()
		{
			CanExecuteChanged?.Invoke(this, null);
		}
	}
}