namespace SharpRemote.Watchdog
{
	public enum Installation
	{
		/// <summary>
		/// Only install the application if there's no previous installation.
		/// If there is, fail the installation.
		/// </summary>
		FailOnUpgrade,

		/// <summary>
		/// If there's a previous installation, kill all of its running processes
		/// and completely remove the installation before attempting a new install.
		/// </summary>
		CleanInstall,

		/// <summary>
		/// If there's a previous installation, kill all of its running processes
		/// and then apply the installation.
		/// </summary>
		/// <remarks>
		/// Files not touched by the current installation are simply from the previous
		/// installation.
		/// </remarks>
		ColdUpdate,

		/// <summary>
		/// Simply apply the installation, even if there's an installation and don't kill
		/// its running processes.
		/// </summary>
		/// <remarks>
		/// Files not touched by the current installation are simply from the previous
		/// installation.
		/// </remarks>
		/// <remarks>
		/// The installation will fail if the files being installed are in used.
		/// </remarks>
		HotUpdate,
	}
}