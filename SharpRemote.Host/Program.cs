using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using SharpRemote.Hosting;
using log4net;
using log4net.Config;
using SharpRemote.CodeGeneration;

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
				var log4NetConfigFileInfo = new FileInfo("SharpRemote.Host.exe.config");
				if (!log4NetConfigFileInfo.Exists)
					log4NetConfigFileInfo = new FileInfo("SharpRemote.Host.dll.config");

				XmlConfigurator.Configure(log4NetConfigFileInfo);

				AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

				using (var silo = new OutOfProcessSiloServer(args))
				{
					silo.Run(IPAddress.Loopback);
				}
				
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception, terminating...: {0}", e);

				OutOfProcessSiloServer.ReportException(e);
			}
		}

		private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Log.ErrorFormat("Caught unhandled exception: {0}", e.ExceptionObject);
		}
	}
}