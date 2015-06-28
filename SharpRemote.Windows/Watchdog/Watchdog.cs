using System;

namespace SharpRemote.Watchdog
{
	public sealed class Watchdog
		: IWatchdog
	{
		private readonly IInternalWatchdog _internalWatchdog;

		public Watchdog(IInternalWatchdog internalWatchdog)
		{
			_internalWatchdog = internalWatchdog;
		}

		public void RegisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");

			_internalWatchdog.RegisterApplicationInstance(instance);
		}

		public void UnregisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			if (instance == null) throw new ArgumentNullException("instance");
			if (instance.Name == null) throw new ArgumentNullException("instance.Name");

			_internalWatchdog.UnregisterApplicationInstance(instance.Name);
		}

		public void UninstallApplication(InstalledApplication application)
		{
			if (application == null) throw new ArgumentNullException("application");

			_internalWatchdog.RemoveApplication(application.Descriptor.Name);
		}

		public IApplicationInstaller StartInstallation(ApplicationDescriptor description, Installation installation = Installation.FailOnUpgrade)
		{
			return new ApplicationInstaller(_internalWatchdog, description, installation);
		}
	}
}