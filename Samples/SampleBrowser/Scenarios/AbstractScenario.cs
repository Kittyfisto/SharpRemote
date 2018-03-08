using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using log4net.Core;
using SampleBrowser.Scenarios.Host;

namespace SampleBrowser.Scenarios
{
	public abstract class AbstractScenario
		: IScenario
	{
		private readonly string _description;
		private readonly ICommand _startCommand;
		private readonly ICommand _stopCommand;
		private readonly string _title;

		protected AbstractScenario(string title, string description, bool isEnabled = true)
		{
			Output = new ObservableCollection<string>();
			_title = title;
			_description = description;

			_startCommand = new DelegateCommand(Start)
			{
				CanBeExecuted = isEnabled
			};
			_stopCommand = new DelegateCommand(Stop);
		}

		private static Dispatcher Dispatcher => App.Dispatcher;

		public ObservableCollection<string> Output { get; }

		public ICommand RunTestCommand
		{
			get { return new TaskCommand(() => Task.Factory.StartNew(RunTestHost)); }
		}

		public string Title => _title;

		public string Description => _description;

		public ICommand StartCommand => _startCommand;

		public ICommand StopCommand => _stopCommand;

		protected void Log(LoggingEvent @event)
		{
			Log(@event.RenderedMessage);
		}

		protected void Log(string that)
		{
			App.Dispatcher.BeginInvoke(new Action(() => Output.Add(that)));
		}

		public abstract FrameworkElement CreateView();

		private async void Start(object unused)
		{
			App.ViewModel.ShowScenario(this);
			await Start();
			await Dispatcher.BeginInvoke(new Action(ScenarioStarted));
		}

		private void RunTestHost()
		{
			Log("Starting test...");
			try
			{
				if (RunTest())
					Log("Test succeeded!");
				else
					Log("Test failed");
			}
			catch (Exception e)
			{
				Log(string.Format("Test failed: {0}", e.Message));
				Log(string.Format("{0}", e.GetType()));
				Log(string.Format("{0}", e.TargetSite));
				Log(e.StackTrace);
			}
		}

		protected abstract bool RunTest();


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