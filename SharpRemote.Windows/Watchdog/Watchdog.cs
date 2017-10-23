using System;

namespace SharpRemote.Watchdog
{
	/// <summary>
	/// </summary>
	public sealed class Watchdog
		: IWatchdog
	{
		private readonly IInternalWatchdog _internalWatchdog;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="internalWatchdog"></param>
		public Watchdog(IInternalWatchdog internalWatchdog)
		{
			_internalWatchdog = internalWatchdog;
		}

		/// <inheritdoc />
		public void RegisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			if (instance == null) throw new ArgumentNullException(nameof(instance));

			_internalWatchdog.RegisterApplicationInstance(instance);
		}

		/// <inheritdoc />
		public void UnregisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			if (instance == null) throw new ArgumentNullException(nameof(instance));
			if (instance.Name == null) throw new ArgumentNullException("instance.Name");

			_internalWatchdog.UnregisterApplicationInstance(instance.Name);
		}

		/// <inheritdoc />
		public void UninstallApplication(InstalledApplication application)
		{
			if (application == null) throw new ArgumentNullException(nameof(application));

			_internalWatchdog.RemoveApplication(application.Descriptor.Name);
		}

		/// <inheritdoc />
		public IApplicationInstaller StartInstallation(ApplicationDescriptor description,
			Installation installation = Installation.FailOnUpgrade)
		{
			return new ApplicationInstaller(_internalWatchdog, description, installation);
		}
	}
}