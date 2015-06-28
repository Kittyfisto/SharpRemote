using System;
using log4net.Config;

namespace SharpRemote.Watchdog
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			BasicConfigurator.Configure();

			Console.WriteLine("Starting watchdog...");
			using (var host = new WatchdogHost())
			{
				Console.WriteLine("Running and listening on {0}", host.LocalEndPoint);
				Console.WriteLine("Name published via PNRP");
				Console.WriteLine("Type exit to end the watchdog");

				while (Console.ReadLine() != "exit")
					break;
			}
		}
	}
}