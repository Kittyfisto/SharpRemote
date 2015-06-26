using System;

namespace SharpRemote.Watchdog
{
	/// <summary>
	/// Internal interface to control the remote watchdog.
	/// This will actually be remoted.
	/// </summary>
	public interface IRemoteWatchdog
	{
		/// <summary>
		/// Registers the given process with this monitor.
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		long RegisterApplicationInstance(ApplicationInstanceDescription instance);

		void UnregisterApplicationInstance(long id);

		long StartInstallation(ApplicationDescriptor description);
		InstalledApplication CommitInstallation(long appId);
		void AbortInstallation(long appId);
		void RemoveApplication(long id);

		/// <summary>
		/// Creates a new file (or replaces an existing file) at the given location
		/// and reserves the given amount of space.
		/// </summary>
		/// <param name="appId"></param>
		/// <param name="folder"></param>
		/// <param name="fileName"></param>
		/// <param name="fileSize"></param>
		/// <returns></returns>
		long CreateFile(long appId, Environment.SpecialFolder folder, string fileName, long fileSize);
		void WriteFilePartially(long fileId, byte[] content, int offset, int length);

		void WriteFile(long appId, Environment.SpecialFolder folder, string fileName, byte[] content);
		void DeleteFile(long appId, Environment.SpecialFolder folder, string fileName);
	}
}