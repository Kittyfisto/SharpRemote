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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="instanceName"></param>
		void UnregisterApplicationInstance(string instanceName);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="description"></param>
		/// <param name="installation"></param>
		void StartInstallation(ApplicationDescriptor description, Installation installation = Installation.FailOnUpgrade);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="applicationName"></param>
		/// <returns></returns>
		InstalledApplication CommitInstallation(string applicationName);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="appId"></param>
		void AbortInstallation(string appId);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
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

		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileId"></param>
		/// <param name="content"></param>
		/// <param name="offset"></param>
		/// <param name="length"></param>
		void WriteFilePartially(long fileId, byte[] content, int offset, int length);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="folder"></param>
		/// <param name="fileName"></param>
		/// <param name="content"></param>
		void WriteFile(string applicationName, Environment.SpecialFolder folder, string fileName, byte[] content);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="folder"></param>
		/// <param name="fileName"></param>
		void DeleteFile(string applicationName, Environment.SpecialFolder folder, string fileName);
	}
}