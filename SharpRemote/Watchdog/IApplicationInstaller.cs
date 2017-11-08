using System;
using System.Collections.Generic;

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
		/// Adds the given file to the current installation.
		/// </summary>
		/// <param name="sourceFileName"></param>
		/// <param name="destinationFolder"></param>
		/// <param name="destinationPath"></param>
		void AddFile(string sourceFileName, Environment.SpecialFolder destinationFolder, string destinationPath = null);

		/// <summary>
		/// Adds all files in the given folder to the current installation.
		/// </summary>
		/// <remarks>
		/// Doesn't add files in sub-directories of the given folder.
		/// </remarks>
		/// <param name="sourceFolder"></param>
		/// <param name="destinationFolder"></param>
		/// <param name="destinationPath"></param>
		void AddFiles(string sourceFolder, Environment.SpecialFolder destinationFolder, string destinationPath = null);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="files"></param>
		/// <param name="destinationFolder"></param>
		/// <param name="destinationPath"></param>
		void AddFiles(IEnumerable<string> files, Environment.SpecialFolder destinationFolder, string destinationPath = null);

		/// <summary>
		/// Finishes the installation - blocks until all files have been transferred and successfully installed
		/// on the target system or an exception is thrown in case of failure.
		/// </summary>
		InstalledApplication Commit();

	}
}