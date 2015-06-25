namespace SampleBrowser
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow()
		{
			App.Dispatcher = Dispatcher;
			App.ViewModel = new MainWindowViewModel();

			InitializeComponent();
			DataContext = App.ViewModel;
		}
	}
}