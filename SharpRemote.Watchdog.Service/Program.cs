using System.ServiceProcess;

namespace SharpRemote.Watchdog.Service
{
	internal static class Program
	{
		/// <summary>
		///     The main entry point for the application.
		/// </summary>
		private static void Main()
		{
			var servicesToRun = new ServiceBase[]
				{
					new WatchdogService()
				};
			ServiceBase.Run(servicesToRun);
		}
	}
}