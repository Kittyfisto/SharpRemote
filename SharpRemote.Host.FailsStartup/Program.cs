using System;
using System.IO;
using SharpRemote.Hosting;

namespace SharpRemote.Host.FailsStartup
{
	internal class Program
	{
		private static void Main()
		{
			try
			{
				throw new FileNotFoundException("Shit happens", "Important File.dat");
			}
			catch (Exception e)
			{
				OutOfProcessSiloServer.ReportException(e);
			}
		}
	}
}