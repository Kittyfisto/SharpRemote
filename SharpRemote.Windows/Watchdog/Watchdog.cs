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

			_remoteWatchdog.RegisterApplicationInstance(instance);
		}

		public void UnregisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			if (instance.Id == null) throw new ArgumentNullException("instance.Id");

			_remoteWatchdog.UnregisterApplicationInstance(instance.Id.Value);
		}

		public void UninstallApplication(ApplicationDescriptor description)
		{
			if (description == null) throw new ArgumentNullException("description");
			if (description.Id == null) throw new ArgumentNullException("description.Id");

			_remoteWatchdog.RemoveApplication(description.Id.Value);
		}

		public IApplicationInstaller StartInstallation(ApplicationDescriptor description)
		{
			return new ApplicationInstaller(_remoteWatchdog, description);
		}
	}
}