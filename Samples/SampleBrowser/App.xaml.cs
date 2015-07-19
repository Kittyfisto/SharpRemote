using System;
using System.Windows.Threading;
using log4net;
using log4net.Config;
using log4net.Core;

namespace SampleBrowser
{
	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		public static new Dispatcher Dispatcher;
		public static MainWindowViewModel ViewModel;

		public App()
		{
			BasicConfigurator.Configure();

			((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).Root.Level = Level.Info;
			((log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository()).RaiseConfigurationChanged(EventArgs.Empty);
		}
	}
}