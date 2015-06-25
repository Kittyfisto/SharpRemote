using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SampleBrowser.Scenarios.Host;

namespace SampleBrowser.Scenarios
{
	public abstract class AbstractScenario
		: IScenario
	{
		private readonly string _description;
		private readonly ObservableCollection<string> _output;
		private readonly ICommand _startCommand;
		private readonly ICommand _stopCommand;
		private readonly string _title;

		protected AbstractScenario(string title, string description)
		{
			_output = new ObservableCollection<string>();
			_title = title;
			_description = description;

			_startCommand = new DelegateCommand(Start);
			_stopCommand = new DelegateCommand(Stop);
		}

		private static Dispatcher Dispatcher
		{
			get { return App.Dispatcher; }
		}

		public ObservableCollection<string> Output
		{
			get { return _output; }
		}

		public ICommand RunTestCommand
		{
			get { return new TaskCommand(() => Task.Factory.StartNew(RunTestHost)); }
		}

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

		protected void Log(string that)
		{
			App.Dispatcher.BeginInvoke(new Action(() => _output.Add(that)));
		}

		public abstract FrameworkElement CreateView();

		private void Start(object unused)
		{
			App.ViewModel.ShowScenario(this);
			Start().ContinueWith(task => Dispatcher.BeginInvoke(new Action(ScenarioStarted)));
		}

		private void RunTestHost()
		{
			Log("Starting test...");
			try
			{
				RunTest();
				Log("Test succeeded!");
			}
			catch (Exception e)
			{
				Log(string.Format("Test failed: {0}", e.Message));
				Log(string.Format("{0}", e.GetType()));
				Log(string.Format("{0}", e.TargetSite));
				Log(e.StackTrace);
			}
		}

		protected abstract void RunTest();


		private void ScenarioStarted()
		{
		}

		private void Stop(object unused)
		{
			Stop();
		}

		protected abstract Task Start();
		protected abstract Task Stop();
	}
}