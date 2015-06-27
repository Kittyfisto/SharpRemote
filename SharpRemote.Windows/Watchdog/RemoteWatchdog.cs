using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.Hosting;

namespace SharpRemote.Watchdog
{
	internal sealed class RemoteWatchdog
		: IRemoteWatchdog
		  , IDisposable
	{
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly Dictionary<long, InstalledApplication> _installedApplications;
		private readonly Dictionary<long, Stream> _openedFiles;
		private readonly Dictionary<long, InstalledApplication> _pendingInstallations;
		private readonly Dictionary<long, Process> _processes;
		private readonly Dictionary<long, ApplicationInstanceDescription> _registeredApplicationInstances;
		private readonly object _syncRoot;
		private readonly Task _task;
		private long _nextAppId;
		private long _nextApplicationInstanceId;
		private long _nextFileId;

		public RemoteWatchdog()
		{
			_syncRoot = new object();
			_openedFiles = new Dictionary<long, Stream>();
			_pendingInstallations = new Dictionary<long, InstalledApplication>();
			_installedApplications = new Dictionary<long, InstalledApplication>();

			_registeredApplicationInstances = new Dictionary<long, ApplicationInstanceDescription>();

			_processes = new Dictionary<long, Process>();

			_cancellationTokenSource = new CancellationTokenSource();
			_syncRoot = new object();

			_task = Task.Factory.StartNew(MonitorApplications, TaskCreationOptions.LongRunning);
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
			}
		}

		public void Unregister(long id)
		{
			lock (_syncRoot)
			{
				StopInstance(id);
				_registeredApplicationInstances.Remove(id);
			}
		}

		public void StartInstance(long instanceId)
		{
			lock (_syncRoot)
			{
				if (!_processes.ContainsKey(instanceId))
				{
					Process process = StartNewProcress(instanceId);
					_processes.Add(instanceId, process);
					try
					{
						process.Exited += ProcessOnExited;
						process.Start();
					}
					catch (Exception)
					{
						process.Exited -= ProcessOnExited;
						_processes.Remove(instanceId);
						throw;
					}
				}
			}
		}

		private void ProcessOnExited(object sender, EventArgs eventArgs)
		{
		}

		private Process StartNewProcress(long instanceId)
		{
			lock (_syncRoot)
			{
				ApplicationInstanceDescription instance = _registeredApplicationInstances[instanceId];
				InstalledFile executable = instance.Executable;
				InstalledApplication application = _installedApplications[instance.AppId];

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

		private void StartAllApplicationInstances(long applicationId)
		{
			lock (_syncRoot)
			{
				List<long> instanceIds = _registeredApplicationInstances.Where(x => x.Value.AppId == applicationId).Select(x => x.Key).ToList();
				foreach (long instanceId in instanceIds)
				{
					StartInstance(instanceId);
				}
			}
		}

		/// <summary>
		///     Stops all application instances belonging to the given application.
		/// </summary>
		/// <param name="applicationId"></param>
		private void StopAllApplicationInstances(long applicationId)
		{
			lock (_syncRoot)
			{
				List<long> instanceIds =
					_processes.Keys.Where(x => _registeredApplicationInstances[x].AppId == applicationId).ToList();
				foreach (long instanceId in instanceIds)
				{
					StopInstance(instanceId);
				}
			}
		}

		/// <summary>
		///     Stops the application instance with the given id.
		/// </summary>
		/// <param name="instanceId"></param>
		private void StopInstance(long instanceId)
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

		public long RegisterApplicationInstance(ApplicationInstanceDescription instance)
		{
			lock (_syncRoot)
			{
				long id = ++_nextApplicationInstanceId;
				_registeredApplicationInstances.Add(id, instance);
				try
				{
					StartInstance(id);
					instance.Id = id;
					return id;
				}
				catch (Exception)
				{
					_registeredApplicationInstances.Remove(id);
					throw;
				}
			}
		}

		public void UnregisterApplicationInstance(long id)
		{
			lock (_syncRoot)
			{
				ApplicationInstanceDescription description;
				if (_registeredApplicationInstances.TryGetValue(id, out description))
				{
					StopInstance(id);
					_registeredApplicationInstances.Remove(id);
				}
			}
		}

		public long StartInstallation(ApplicationDescriptor description, Installation installation)
		{
			lock (_syncRoot)
			{
				// If there's another pending installation with the same folder then we'll bail early...
				InstalledApplication pending =
					_pendingInstallations.Values.FirstOrDefault(x => x.Descriptor.FolderName == description.FolderName);
				if (pending != null)
					throw new InstallationFailedException(
						string.Format(
							"There already is a pending installation for the same application - this installation must be completed or aborted in order for a new installation to be allowed"));

				// Let's find out if we're replacing an existing installation...
				InstalledApplication existingApp =
					_installedApplications.Values.FirstOrDefault(x => x.Descriptor.FolderName == description.FolderName);
				InstalledApplication newApp;
				if (existingApp != null)
				{
					switch (installation)
					{
						case Installation.FailOnUpgrade:
							throw new InstallationFailedException(
								string.Format("There already is an installation of the same application present"));

						case Installation.CleanInstall:
							StopAllApplicationInstances(existingApp.Id);
							RemoveApplication(existingApp.Id);
							newApp = new InstalledApplication(_nextAppId + 1, description);
							++_nextAppId;
							break;

						case Installation.ColdUpdate:
							StopAllApplicationInstances(existingApp.Id);
							newApp = new InstalledApplication(existingApp.Id, description);
							newApp.Files.AddRange(existingApp.Files);
							break;

						case Installation.HotUpdate:
							newApp = new InstalledApplication(existingApp.Id, description);
							newApp.Files.AddRange(existingApp.Files);
							break;

						default:
							throw new InvalidEnumArgumentException("installation", (int) installation, typeof (Installation));
					}
				}
				else
				{
					newApp = new InstalledApplication(_nextAppId + 1, description);
					++_nextAppId;
				}

				_pendingInstallations.Add(newApp.Id, newApp);
				return newApp.Id;
			}
		}

		public InstalledApplication CommitInstallation(long appId)
		{
			lock (_syncRoot)
			{
				InstalledApplication newApp = _pendingInstallations[appId];
				foreach (InstalledFile file in newApp.Files)
				{
					Stream stream;
					if (_openedFiles.TryGetValue(file.Id, out stream))
					{
						stream.Dispose();
						_openedFiles.Remove(file.Id);
					}
				}

				_pendingInstallations.Remove(appId);
				_installedApplications[appId] = newApp;

				StartAllApplicationInstances(newApp.Id);

				return newApp;
			}
		}

		public void AbortInstallation(long appId)
		{
			throw new NotImplementedException();
		}

		public void RemoveApplication(long id)
		{
			lock (_syncRoot)
			{
				RemoveApplicationInstances(id);
			}
		}

		public long CreateFile(long appId, Environment.SpecialFolder folder, string fileName, long fileSize)
		{
			InstalledApplication app = _pendingInstallations[appId];
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

		public void WriteFile(long appId, Environment.SpecialFolder folder, string fileName, byte[] content)
		{
			InstalledApplication app = _pendingInstallations[appId];
			string path = Resolve(app, folder, fileName);

			if (File.Exists(path))
				File.Delete(path);
			File.WriteAllBytes(path, content);

			CreateAndAddFileDescription(app, folder, fileName, content.Length);
		}

		public void DeleteFile(long appId, Environment.SpecialFolder folder, string fileName)
		{
			InstalledApplication app = _installedApplications[appId];
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
		/// <param name="applicationId"></param>
		private void RemoveApplicationInstances(long applicationId)
		{
			lock (_syncRoot)
			{
				List<KeyValuePair<long, ApplicationInstanceDescription>> instances =
					_registeredApplicationInstances.Where(x => x.Value.AppId == applicationId).ToList();
				foreach (var instance in instances)
				{
					UnregisterApplicationInstance(instance.Key);
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
			return Resolve(app.Descriptor.FolderName, folder, relativeFileName);
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