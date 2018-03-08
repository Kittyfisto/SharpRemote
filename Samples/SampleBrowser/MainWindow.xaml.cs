using System;

namespace SampleBrowser
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		private readonly MainWindowViewModel _viewModel;

		public MainWindow()
		{
			App.Dispatcher = Dispatcher;
			App.ViewModel = _viewModel = new MainWindowViewModel();

			InitializeComponent();
			DataContext = App.ViewModel;

			Closed += OnClosed;
		}

		private void OnClosed(object sender, EventArgs eventArgs)
		{
			_viewModel.CurrentScenario?.Stop();
		}
	}
}