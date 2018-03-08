using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using SharpRemote.Hosting;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	public sealed class LongTermScenario
		: AbstractScenario
	{
		private readonly RollingFileAppender _fileAppender;
		private volatile bool _running;

		public LongTermScenario()
			: base(
				"Long Term Usage",
				"Publishes an application to a (remote) watchdog and performs long-term real world tests"
				)
		{
			var patternLayout = new PatternLayout
			{
				ConversionPattern = "%date [%thread] %-5level %logger - %message%newline"
			};
			patternLayout.ActivateOptions();

			var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			var logFilePath = Path.Combine(appData, "SharpRemote", "SampleBrowser", "LongTermUsage.log");

			_fileAppender = new RollingFileAppender
			{
				AppendToFile = false,
				File = logFilePath,
				Layout = patternLayout,
				MaxSizeRollBackups = 20,
				MaximumFileSize = "1GB",
				RollingStyle = RollingFileAppender.RollingMode.Size,
				StaticLogFileName = false
			};
		}

		public override FrameworkElement CreateView()
		{
			return new LongTermView();
		}

		protected override bool RunTest()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var dataLogger = silo.CreateGrain<IDataListener>(typeof(DataLogger));
				var blob = new byte[1024];
				int packetIndex = 0;

				while (_running)
				{
					dataLogger.Process(new DataPacket
					{
						Data = blob,
						SequenceNumber = ++packetIndex
					});
				}
			}

			return true;
		}

		protected override Task StartAsync()
		{
			return Task.Factory.StartNew(() =>
			{
				var hierarchy = (Hierarchy) LogManager.GetRepository();
				_fileAppender.ActivateOptions();
				hierarchy.Root.AddAppender(_fileAppender);
				hierarchy.Root.Level = Level.Info;

				var logger = (Logger)hierarchy.GetLogger("SharpRemote.LatencyMonitor");
				logger.Level = Level.Debug;

				logger = (Logger)hierarchy.GetLogger("SharpRemote.HeartbeatMonitor");
				logger.Level = Level.Debug;

				hierarchy.Configured = true;
				_running = true;
			});
		}

		protected override Task StopAsync()
		{
			return Task.Factory.StartNew(() =>
			{
				var hierarchy = (Hierarchy) LogManager.GetRepository();
				hierarchy.Root.RemoveAppender(_fileAppender);
				hierarchy.Configured = true;

				_running = false;
			});
		}
	}
}