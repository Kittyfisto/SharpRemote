using System;
using System.Threading.Tasks;
using System.Windows;
using SharpRemote;
using SharpRemote.Watchdog;

namespace SampleBrowser.Scenarios.WatchdogInstallation
{
	public sealed class RemoteHostScenario
		: AbstractScenario
	{
		public RemoteHostScenario()
			: base(
			"Remote Installation",
			"Install an application over the network on a remote machine"
			)
		{
			
		}

		public override FrameworkElement CreateView()
		{
			return new RemoteInstallView();
		}

		protected override bool RunTest()
		{
			using (var accessor = new LogInterceptor(Log))
			using (var endPoint = new SocketRemotingEndPointClient())
			{
				endPoint.Connect(WatchdogHost.PeerName, TimeSpan.FromSeconds(5));

				var remote = endPoint.CreateProxy<IInternalWatchdog>(WatchdogHost.ObjectId);
				var watchdog = new Watchdog(remote);

				var app = new ApplicationDescriptor
					{
						Name = "SharpRemote 0.1 Developer Build"
					};
				using (var installer = watchdog.StartInstallation(app, Installation.CleanInstall))
				{
					installer.AddFiles(new[]
						{
							// Full deployment
							"SharpRemote.dll",
							"SharpRemote.pdb",
							"SharpRemote.Watchdog.exe",
							"SharpRemote.Watchdog.exe.config",
							"SharpRemote.Watchdog.pdb",
							"SharpRemote.Watchdog.Service.exe",
							"SharpRemote.Watchdog.Service.exe.config",
							"SharpRemote.Watchdog.Service.pdb",
							"SharpRemote.Host.exe",
							"SharpRemote.Host.exe.config",
							"SharpRemote.Host.pdb",

							// 3rd party
							"log4net.dll"
						},
						Environment.SpecialFolder.LocalApplicationData);

					installer.Commit();
				}

				return true;
			}
		}

		protected override Task Start()
		{
			return Task.Factory.StartNew(() =>
				{

				});
		}

		protected override Task Stop()
		{
			return Task.Factory.StartNew(() =>
			{

			});
		}
	}
}