namespace SharpRemote.Watchdog
{
	/// <summary>
	///     A watchdog has the following responsibilites:
	///     - Install / Update / Uninstall applications remotely
	///     - Start / Stop installed applications remotely
	///     - Monitor local application health and execute failure strategies (restart, etc...)
	/// </summary>
	public interface IWatchdog
	{
		#region Monitoring

		/// <summary>
		/// Registers an application instance with the watchdog.
		/// From now on, the described application will be started and monitored as configured
		/// for as long as the watchdog is running.
		/// </summary>
		/// <param name="instance"></param>
		void RegisterApplicationInstance(ApplicationInstanceDescription instance);

		/// <summary>
		/// Unregisters thegiven application instance with the watchdog.
		/// If the application in question is running, it will be killed immediately.
		/// </summary>
		/// <param name="instance"></param>
		void UnregisterApplicationInstance(ApplicationInstanceDescription instance);

		#endregion

		#region Installation

		/// <summary>
		/// Starts the installation of an application defined by the given descriptor.
		/// Can be used to:
		/// - Install a completely new application
		/// - Upgrade an existing application (the new version may be installed on top or besides the existing app)
		/// - Update an existing application
		/// </summary>
		/// <remarks>
		/// The installation is only completed once <see cref="IApplicationInstaller.Commit"/> is called on the returned
		/// object.
		/// </remarks>
		/// <remarks>
		/// The installation is aborted if:
		/// - The returned <see cref="IApplicationInstaller"/> is disposed of before <see cref="IApplicationInstaller.Commit"/> is called
		/// - the connection to the watchdog is interrupted (for any reason)
		/// - the watchdog is shut down (doesn't matter if the shutdown is intentional, due to power loss, etc..)
		/// 
		/// If the installation was aborted, then the watchdog tries to restore the system to a consistent state as soon as possible.
		/// </remarks>
		/// <returns></returns>
		IApplicationInstaller StartInstallation(ApplicationDescriptor description, Installation installation = Installation.FailOnUpgrade);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="description"></param>
		void UninstallApplication(InstalledApplication description);

		#endregion
	}
}