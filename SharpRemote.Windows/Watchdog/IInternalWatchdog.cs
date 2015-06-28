using System;

namespace SharpRemote.Watchdog
{
	/// <summary>
	/// Internal interface to control the remote watchdog.
	/// This will actually be remoted.
	/// </summary>
	public interface IInternalWatchdog
	{
		/// <summary>
		/// Registers the given process with this monitor.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		void RegisterApplicationInstance(ApplicationInstanceDescription instance);

		void UnregisterApplicationInstance(string instanceName);

		void StartInstallation(ApplicationDescriptor description, Installation installation = Installation.FailOnUpgrade);
		InstalledApplication CommitInstallation(string applicationName);
		void AbortInstallation(string appId);
		void RemoveApplication(string id);

		/// <summary>
		/// Creates a new file (or replaces an existing file) at the given location
		/// and reserves the given amount of space.
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="folder"></param>
		/// <param name="fileName"></param>
		/// <param name="fileSize"></param>
		/// <returns></returns>
		long CreateFile(string applicationName, Environment.SpecialFolder folder, string fileName, long fileSize);
		void WriteFilePartially(long fileId, byte[] content, int offset, int length);

		void WriteFile(string applicationName, Environment.SpecialFolder folder, string fileName, byte[] content);
		void DeleteFile(string applicationName, Environment.SpecialFolder folder, string fileName);
	}
}