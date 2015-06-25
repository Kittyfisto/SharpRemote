using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace SampleBrowser.Scenarios
{
	public abstract class AbstractScenario
		: IScenario
	{
		private static Dispatcher Dispatcher
		{
			get { return App.Dispatcher; }
		}

		private readonly string _title;
		private readonly string _description;
		private readonly ICommand _startCommand;
		private readonly ICommand _stopCommand;

		protected AbstractScenario(string title, string description)
		{
			_title = title;
			_description = description;

			_startCommand = new DelegateCommand(Start);
			_stopCommand = new DelegateCommand(Stop);
		}

		public abstract FrameworkElement CreateView();

		private void Start(object unused)
		{
			App.ViewModel.ShowScenario(this);
			Start().ContinueWith(task => Dispatcher.BeginInvoke(new Action(ScenarioStarted)));
		}

		private void ScenarioStarted()
		{
			
		}

		private void Stop(object unused)
		{
			Stop();
		}

		protected abstract Task Start();
		protected abstract Task Stop();

		public string Title
		{
			get { return _title; }
		}

		public string Description
		{
			get { return _description; }
		}

		public ICommand StartCommand
		{
			get { return _startCommand; }
		}

		public ICommand StopCommand
		{
			get { return _stopCommand; }
		}
	}
}