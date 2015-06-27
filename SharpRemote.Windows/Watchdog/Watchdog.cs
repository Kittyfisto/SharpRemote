using System;

namespace SharpRemote.Watchdog
{
	public sealed class Watchdog
		: IWatchdog
	{
		private readonly IRemoteWatchdog _remoteWatchdog;

		public Watchdog(IRemoteWatchdog remoteWatchdog)
		{
			_remoteWatchdog = remoteWatchdog;
		}

		public void RegisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			var id = _remoteWatchdog.RegisterApplicationInstance(instance);
			instance.Id = id;
		}

		public void UnregisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			if (instance.Id == null) throw new ArgumentNullException("instance.Id");

			_remoteWatchdog.UnregisterApplicationInstance(instance.Id.Value);
		}

		public void UninstallApplication(InstalledApplication application)
		{
			if (application == null) throw new ArgumentNullException("application");

			_remoteWatchdog.RemoveApplication(application.Id);
		}

		public IApplicationInstaller StartInstallation(ApplicationDescriptor description, Installation installation = Installation.FailOnUpgrade)
		{
			return new ApplicationInstaller(_remoteWatchdog, description, installation);
		}
	}
}