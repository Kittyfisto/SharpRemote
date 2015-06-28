using System;
using System.Net;
using System.Net.PeerToPeer;
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
			using (IRemotingEndPoint endPoint = new SocketEndPoint(IPAddress.Loopback))
			{
				Log("Looking for watchdog...");
				var resolver = new PeerNameResolver();
				var results = resolver.Resolve(new PeerName(WatchdogHost.PeerName, PeerNameType.Unsecured));

				if (results.Count == 0)
				{
					Log("Couldn't find watchdog!");
					return false;
				}

				var peer = results[0];
				var endPoints = peer.EndPointCollection;
				Log(string.Format("Found watchdog, {0} endpoints", endPoints.Count));

				foreach (var ep in endPoints)
				{
					Log(string.Format("Connecting to {0}...", ep));
					var uri = new Uri(string.Format("tcp://{0}", ep));
					try
					{
						endPoint.Connect(uri, TimeSpan.FromSeconds(5));
						Log("Successfully connected to watchdog!");
						break;
					}
					catch (Exception)
					{
						
					}
				}

				if (!endPoint.IsConnected)
				{
					Log("Couldn't establish a connection with the endpoint");
					return false;
				}

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