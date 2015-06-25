using System;
using System.Windows.Input;

namespace SampleBrowser.Scenarios
{
	public class DelegateCommand
		: ICommand
	{
		private readonly Func<object, bool> _canExecute;
		private readonly Action<object> _execute;

		public DelegateCommand(Action<object> execute, Func<object, bool> canExecute = null)
		{
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute == null || _canExecute(parameter);
		}

		public void Execute(object parameter)
		{
			_execute(parameter);
		}

		public event EventHandler CanExecuteChanged;

		public void RaiseCanExecuteChanged()
		{
			var fn = CanExecuteChanged;
			if (fn != null)
				fn(this, null);
		}
	}
}