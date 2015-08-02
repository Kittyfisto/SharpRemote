using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using SharpRemote.Hosting;
using log4net;
using log4net.Config;

namespace SharpRemote.Host
{
	internal class Program
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static void Main(string[] args)
		{
			try
			{
				GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;
				XmlConfigurator.Configure(new FileInfo("SharpRemote.Host.exe.config"));

				AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

				using (var silo = new OutOfProcessSiloServer(args))
				{
					silo.Run();
				}
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception, terminating...: {0}", e);
			}
		}

		private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Log.ErrorFormat("Caught unhandled exception: {0}", e.ExceptionObject);
		}
	}
}