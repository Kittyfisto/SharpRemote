using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.Extensions;
using log4net;

namespace SharpRemote.Watchdog
{
	internal sealed class InternalWatchdog
		: IInternalWatchdog
		  , IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private const string InstalledApplicationsName = "InstalledApplications";
		private const string ApplicationInstancesName = "ApplicationInstances";

		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly Dictionary<string, InstalledApplication> _installedApplications;
		private readonly Dictionary<string, InstalledApplication> _pendingInstallations;
		private readonly Dictionary<string, Process> _processes;
		private readonly Dictionary<string, ApplicationInstanceDescription> _registeredApplicationInstances;
		private readonly Dictionary<long, Stream> _openedFiles;
		private readonly IIsolatedStorage _storage;
		private readonly object _syncRoot;
		private readonly Task _task;
		private long _nextFileId;

		public InternalWatchdog()
			: this(new IsolatedStorage())
		{}

		public InternalWatchdog(IIsolatedStorage storage)
		{
			if (storage == null) throw new ArgumentNullException(nameof(storage));

			_syncRoot = new object();

			_storage = storage;

			_openedFiles = new Dictionary<long, Stream>();
			_pendingInstallations = new Dictionary<string, InstalledApplication>();
			_installedApplications = new Dictionary<string, InstalledApplication>();
			_registeredApplicationInstances = new Dictionary<string, ApplicationInstanceDescription>();
			_processes = new Dictionary<string, Process>();

			RestoreApplications();

			_cancellationTokenSource = new CancellationTokenSource();
			_syncRoot = new object();

			_task = Task.Factory.StartNew(MonitorApplications, TaskCreationOptions.LongRunning);
		}

		private void RestoreApplications()
		{
			var sw = new Stopwatch();
			sw.Start();

			var apps = _storage.Restore<List<string>>(InstalledApplicationsName);
			if (apps != null)
			{
				foreach (var name in apps)
				{
					var app = _storage.Restore<InstalledApplication>(name);
					if (app != null)
					{
						_installedApplications.Add(app.Name, app);
					}
				}
			}

			var instances = _storage.Restore<List<string>>(ApplicationInstancesName);
			if (instances != null)
			{
				foreach (var name in instances)
				{
					var inst = _storage.Restore<ApplicationInstanceDescription>(name);
					if (inst != null)
					{
						_registeredApplicationInstances.Add(inst.Name, inst);
					}
				}
			}

			sw.Stop();
			var elapsed = sw.Elapsed;
			Log.DebugFormat("Restored descriptions in {0}ms", elapsed.TotalMilliseconds);
		}

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
			_task.Wait();

			lock (_syncRoot)
			{
				foreach (Process proc in _processes.Values)
				{
					proc.TryKill();
				}
				_processes.Clear();
			}
		}

		private void MonitorApplications()
		{
			while (!_cancellationTokenSource.IsCancellationRequested)
			{
				Thread.Sleep(1);
			}
		}

		public void StartInstance(string instanceName)
		{
			lock (_syncRoot)
			{
				if (!_processes.ContainsKey(instanceName))
				{
					Process process = StartNewProcress(instanceName);
					_processes.Add(instanceName, process);
					try
					{
						process.Exited += ProcessOnExited;
						process.Start();
					}
					catch (Exception)
					{
						process.Exited -= ProcessOnExited;
						_processes.Remove(instanceName);
						throw;
					}
				}
			}
		}

		private void ProcessOnExited(object sender, EventArgs eventArgs)
		{
		}

		private Process StartNewProcress(string instanceName)
		{
			lock (_syncRoot)
			{
				ApplicationInstanceDescription instance = _registeredApplicationInstances[instanceName];
				InstalledFile executable = instance.Executable;
				InstalledApplication application = _installedApplications[instance.ApplicationName];

				var process = new Process
					{
						StartInfo = new ProcessStartInfo
							{
								FileName = Resolve(application, executable.Folder, executable.Filename),
							},
						EnableRaisingEvents = true
					};

				return process;
			}
		}

		private void StartAllApplicationInstances(string applicationName)
		{
			lock (_syncRoot)
			{
				List<string> instanceNames = _registeredApplicationInstances.Where(x => x.Value.ApplicationName == applicationName).Select(x => x.Key).ToList();
				foreach (string instanceName in instanceNames)
				{
					StartInstance(instanceName);
				}
			}
		}

		/// <summary>
		///     Stops all application instances belonging to the given application.
		/// </summary>
		/// <param name="applicationName"></param>
		private void StopAllApplicationInstances(string applicationName)
		{
			lock (_syncRoot)
			{
				List<string> instanceNames =
					_processes.Keys.Where(x => _registeredApplicationInstances[x].ApplicationName == applicationName).ToList();
				foreach (string instanceName in instanceNames)
				{
					StopInstance(instanceName);
				}
			}
		}

		/// <summary>
		///     Stops the application instance with the given id.
		/// </summary>
		/// <param name="instanceId"></param>
		private void StopInstance(string instanceId)
		{
			lock (_syncRoot)
			{
				Process process;
				if (_processes.TryGetValue(instanceId, out process))
				{
					process.TryKill();

					_processes.Remove(instanceId);
				}
			}
		}

		#region Installation

		public void RegisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			lock (_syncRoot)
			{
				if (_registeredApplicationInstances.ContainsKey(instance.Name))
				{
					UnregisterApplicationInstance(instance.Name);
				}

				_registeredApplicationInstances.Add(instance.Name, instance);
				try
				{
					StartInstance(instance.Name);
				}
				catch (Exception)
				{
					_registeredApplicationInstances.Remove(instance.Name);
					throw;
				}
			}
		}

		public void UnregisterApplicationInstance(string instanceName)
		{
			lock (_syncRoot)
			{
				ApplicationInstanceDescription description;
				if (_registeredApplicationInstances.TryGetValue(instanceName, out description))
				{
					StopInstance(instanceName);
					_registeredApplicationInstances.Remove(instanceName);
				}
			}
		}

		public void StartInstallation(ApplicationDescriptor description, Installation installation)
		{
			lock (_syncRoot)
			{
				Log.DebugFormat("Starting installation of '{0}': {1}", description.Name, installation);

				// If there's another pending installation with the same folder then we'll bail early...
				InstalledApplication pending =
					_pendingInstallations.Values.FirstOrDefault(x => x.Descriptor.Name == description.Name);
				if (pending != null)
					throw new InstallationFailedException(
						string.Format(
							"There already is a pending installation for the same application - this installation must be completed or aborted in order for a new installation to be allowed"));

				// Let's find out if we're replacing an existing installation...
				InstalledApplication existingApp;
				_installedApplications.TryGetValue(description.Name, out existingApp);
				InstalledApplication newApp;
				if (existingApp != null)
				{
					switch (installation)
					{
						case Installation.FailOnUpgrade:
							throw new InstallationFailedException(
								string.Format("There already is an installation of the same application present"));

						case Installation.CleanInstall:
							StopAllApplicationInstances(existingApp.Name);
							RemoveApplication(existingApp.Name);
							newApp = new InstalledApplication(description);
							break;

						case Installation.ColdUpdate:
							StopAllApplicationInstances(existingApp.Name);
							newApp = new InstalledApplication(description);
							newApp.Files.AddRange(existingApp.Files);
							break;

						case Installation.HotUpdate:
							newApp = new InstalledApplication(description);
							newApp.Files.AddRange(existingApp.Files);
							break;

						default:
							throw new InvalidEnumArgumentException(nameof(installation), (int) installation, typeof (Installation));
					}
				}
				else
				{
					newApp = new InstalledApplication(description);
				}

				_pendingInstallations.Add(newApp.Name, newApp);
			}
		}

		public InstalledApplication CommitInstallation(string applicationName)
		{
			lock (_syncRoot)
			{
				InstalledApplication newApp = _pendingInstallations[applicationName];
				foreach (InstalledFile file in newApp.Files)
				{
					Stream stream;
					if (_openedFiles.TryGetValue(file.Id, out stream))
					{
						stream.Dispose();
						_openedFiles.Remove(file.Id);
					}
				}

				_pendingInstallations.Remove(applicationName);
				_installedApplications[applicationName] = newApp;

				Log.DebugFormat("Installation of '{0}' finished", newApp.Name);

				StartAllApplicationInstances(newApp.Name);

				return newApp;
			}
		}

		public void AbortInstallation(string appId)
		{
			throw new NotImplementedException();
		}

		public void RemoveApplication(string id)
		{
			lock (_syncRoot)
			{
				RemoveApplicationInstances(id);
			}
		}

		public long CreateFile(string applicationName, Environment.SpecialFolder folder, string fileName, long fileSize)
		{
			Log.DebugFormat("Creating file '{0}': {1} bytes", fileName, fileSize);

			InstalledApplication app = _pendingInstallations[applicationName];
			string fname = Resolve(app, folder, fileName);

			var file = CreateAndAddFileDescription(app, folder, fileName, fileSize);

			CreateFolder(fname);
			if (File.Exists(fname))
			{
				File.Delete(fname);
			}

			var fs = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.None);
			fs.SetLength(fileSize);
			lock (_syncRoot)
			{
				_openedFiles.Add(file.Id, fs);
			}
			return file.Id;
		}

		private InstalledFile CreateAndAddFileDescription(InstalledApplication app, Environment.SpecialFolder folder, string fileName, long fileSize)
		{
			var existing =
				app.Files.FirstOrDefault(
					x => string.Equals(fileName, x.Filename, StringComparison.InvariantCultureIgnoreCase) && x.Folder == folder);
			long id;
			if (existing != null)
			{
				app.Files.Remove(existing);
				id = existing.Id;
			}
			else
			{
				id = Interlocked.Increment(ref _nextFileId);
			}

			var file = new InstalledFile
				{
					Id = id,
					FileLength = fileSize,
					Filename = fileName,
					Folder = folder
				};
			app.Files.Add(file);
			return file;
		}

		public void WriteFilePartially(long fileId, byte[] content, int offset, int length)
		{
			Stream stream;
			lock (_syncRoot)
			{
				stream = _openedFiles[fileId];
			}
			stream.Position = offset;
			stream.Write(content, 0, length);
		}

		public void WriteFile(string applicationName, Environment.SpecialFolder folder, string fileName, byte[] content)
		{
			Log.DebugFormat("Creating file '{0}': {1} bytes", fileName, content.Length);

			InstalledApplication app = _pendingInstallations[applicationName];
			string path = Resolve(app, folder, fileName);

			if (File.Exists(path))
				File.Delete(path);
			File.WriteAllBytes(path, content);

			CreateAndAddFileDescription(app, folder, fileName, content.Length);
		}

		public void DeleteFile(string applicationName, Environment.SpecialFolder folder, string fileName)
		{
			Log.DebugFormat("Deleting file '{0}'", fileName);

			InstalledApplication app = _installedApplications[applicationName];
			string path = Resolve(app, folder, fileName);
			File.Delete(path);
		}

		/// <summary>
		///     Removes all registered application instances (and all running applications) from the
		///     application with the given id.
		/// </summary>
		/// <remarks>
		///     Doesn't do anything when there are no instances (or when there's no such application).
		/// </remarks>
		/// <param name="applicationName"></param>
		private void RemoveApplicationInstances(string applicationName)
		{
			lock (_syncRoot)
			{
				List<string> instances =
					_registeredApplicationInstances.Where(x => x.Value.ApplicationName == applicationName).Select(x => x.Key).ToList();
				foreach (var instance in instances)
				{
					UnregisterApplicationInstance(instance);
				}
			}
		}

		private static void CreateFolder(string fname)
		{
			string dir = Path.GetDirectoryName(fname);
			Directory.CreateDirectory(dir);
		}

		[Pure]
		public static string Resolve(InstalledApplication app, Environment.SpecialFolder folder, string relativeFileName)
		{
			return Resolve(app.Name, folder, relativeFileName);
		}

		[Pure]
		public static string Resolve(string appFolder, Environment.SpecialFolder folder, string relativeFileName)
		{
			string folderPath = Environment.GetFolderPath(folder);
			string fullPath = Path.Combine(folderPath, appFolder, relativeFileName);
			return fullPath;
		}

		#endregion
	}
}