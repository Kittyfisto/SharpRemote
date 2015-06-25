using System.Windows.Threading;
using log4net.Config;

namespace SampleBrowser
{
	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		public static Dispatcher Dispatcher;
		public static MainWindowViewModel ViewModel;

		public App()
		{
			BasicConfigurator.Configure();
		}
	}
}