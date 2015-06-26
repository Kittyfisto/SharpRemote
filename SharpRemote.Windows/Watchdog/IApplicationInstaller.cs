using System;

namespace SharpRemote.Watchdog
{
	/// <summary>
	/// Responsible for performing the actual installation.
	/// </summary>
	/// <example>
	/// using (var installer = watchdog.InstallAplication())
	/// {
	///		installer.AddFile(@"C:\Program Files\My Application\App.exe");
	///		installer.AddFile(@"C:\Program Files\My Application\MyConfiguration.exe");
	/// }
	/// </example>
	public interface IApplicationInstaller
		: IDisposable
	{
		/// <summary>
		/// The progress of the installation, range: [0, 1]
		/// </summary>
		double Progress { get; }

		/// <summary>
		/// Adds the list of files to be transferred.
		/// </summary>
		/// <param name="sourceFileName"></param>
		/// <param name="destinationFolder"></param>
		/// <param name="destinationPath"></param>
		void AddFile(string sourceFileName, Environment.SpecialFolder destinationFolder, string destinationPath = null);

		/// <summary>
		/// Finishes the installation - blocks until all files have been transferred and successfully installed
		/// on the target system or an exception is thrown in case of failure.
		/// </summary>
		InstalledApplication Commit();
	}
}