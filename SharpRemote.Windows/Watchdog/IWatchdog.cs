using System.IO;

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
		IApplicationInstaller InstallApplication();
		void UninstallApplication();
	}
}