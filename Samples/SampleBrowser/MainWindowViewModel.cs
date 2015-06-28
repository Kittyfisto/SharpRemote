using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using SampleBrowser.Scenarios;
using SampleBrowser.Scenarios.Host;
using SampleBrowser.Scenarios.WatchdogInstallation;

namespace SampleBrowser
{
	public sealed class MainWindowViewModel
		: INotifyPropertyChanged
	{
		private readonly IScenario[] _scenarios;
		private FrameworkElement _currentScenarioView;
		private bool _isScenarioSelectionVisible;
		private string _title;

		public MainWindowViewModel()
		{
			_title = "Sample Browser";
			_isScenarioSelectionVisible = true;
			_scenarios = new IScenario[]
				{
					new HostScenario(),
					new RemoteHostScenario()
				};
		}

		public string Title
		{
			get { return _title; }
			private set
			{
				if (value == _title)
					return;

				_title = value;
				EmitPropertyChanged();
			}
		}

		public IScenario[] Scenarios
		{
			get { return _scenarios; }
		}

		public FrameworkElement CurrentScenarioView
		{
			get { return _currentScenarioView; }
			private set
			{
				if (value == _currentScenarioView)
					return;

				_currentScenarioView = value;
				EmitPropertyChanged();
			}
		}

		public bool IsScenarioSelectionVisible
		{
			get { return _isScenarioSelectionVisible; }
			private set
			{
				if (value == _isScenarioSelectionVisible)
					return;

				_isScenarioSelectionVisible = value;
				EmitPropertyChanged();
			}
		}

		public void ShowScenario(AbstractScenario scenario)
		{
			CurrentScenarioView = scenario.CreateView();
			CurrentScenarioView.DataContext = scenario;

			IsScenarioSelectionVisible = false;
			Title = string.Format("Sample Browser - {0}", scenario.Title);
		}

		public event PropertyChangedEventHandler PropertyChanged;

		private void EmitPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}