using System;
using System.Collections.Generic;
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
		private long _nextFileId;
		private long _nextAppId;
		private readonly Dictionary<long, Stream> _openedFiles;
		private readonly object _syncRoot;
		private readonly Dictionary<long, InstalledApplication> _pendingInstallations;
		private readonly Dictionary<long, InstalledApplication> _installedApplications;
		private long _nextApplicationInstanceId;
		private readonly Dictionary<long, ApplicationInstanceDescription> _registeredApplicationInstances;
		private readonly Dictionary<long, Process> _processes;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly Task _task;

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

		public void StartApplication(long applicationId)
		{
			lock (_syncRoot)
			{
				var process = StartNewProcress(applicationId);
				_processes.Add(applicationId, process);
				try
				{
					process.Exited += ProcessOnExited;
					process.Start();
				}
				catch (Exception)
				{
					process.Exited -= ProcessOnExited;
					_processes.Remove(applicationId);
					throw;
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
				var instance = _registeredApplicationInstances[instanceId];
				var executable = instance.Executable;
				var application = _installedApplications[instance.AppId];

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

		/// <summary>
		/// Stops all application instances belonging to the given application.
		/// </summary>
		/// <param name="applicationId"></param>
		private void StopApplication(long applicationId)
		{
			lock (_syncRoot)
			{
				var instanceIds = _processes.Keys.Where(x => _registeredApplicationInstances[x].AppId == applicationId).ToList();
				foreach (var instanceId in instanceIds)
				{
					StopInstance(instanceId);
				}
			}
		}

		/// <summary>
		/// Stops the application instance with the given id.
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
				var id = ++_nextApplicationInstanceId;
				_registeredApplicationInstances.Add(id, instance);
				try
				{
					StartApplication(id);
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

		public long StartInstallation(ApplicationDescriptor description)
		{
			lock (_syncRoot)
			{
				// If there's another pending installation with the same folder then we'll bail early...
				var pending = _pendingInstallations.Values.FirstOrDefault(x => x.Descriptor.FolderName == description.FolderName);
				if (pending != null)
					throw new InstallationFailedException(string.Format("There already is a pending installation for the same application"));

				// Let's find out if we're replacing an existing installation...
				var existing = _installedApplications.Values.FirstOrDefault(x => x.Descriptor.FolderName == description.FolderName);
				if (existing != null)
				{
					// The same application is already installed.
					// Performing an update is more interesting because the application could be in use
					// We will have to kill all existing processes and put the monitor on hold (so he won't
					// start new ones) until the installation is finished).
					StopApplication(existing.Id);

					// Furthermore, we'll simply remove the old installation
					RemoveApplication(existing.Id);
				}

				var id = Interlocked.Increment(ref _nextAppId);
				_pendingInstallations.Add(id, new InstalledApplication(id, description));
				return id;
			}
		}

		public InstalledApplication CommitInstallation(long appId)
		{
			lock (_syncRoot)
			{
				var installation = _pendingInstallations[appId];
				foreach (var file in installation.Files)
				{
					var stream = _openedFiles[file.Id];
					stream.Dispose();
					_openedFiles.Remove(file.Id);
				}

				_pendingInstallations.Remove(appId);
				_installedApplications.Add(appId, installation);
				return installation;
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

		/// <summary>
		/// Removes all registered application instances (and all running applications) from the
		/// application with the given id.
		/// </summary>
		/// <remarks>
		/// Doesn't do anything when there are no instances (or when there's no such application).
		/// </remarks>
		/// <param name="applicationId"></param>
		private void RemoveApplicationInstances(long applicationId)
		{
			lock (_syncRoot)
			{
				var instances = _registeredApplicationInstances.Where(x => x.Value.AppId == applicationId).ToList();
				foreach (var instance in instances)
				{
					UnregisterApplicationInstance(instance.Key);
				}
			}
		}

		public long CreateFile(long appId, Environment.SpecialFolder folder, string fileName, long fileSize)
		{
			var app = _pendingInstallations[appId];
			var fname = Resolve(app, folder, fileName);
			var id = Interlocked.Increment(ref _nextFileId);

			var file = new InstalledFile
				{
					Id = id,
					FileLength = fileSize,
					Filename = fileName,
					Folder = folder
				};
			app.Files.Add(file);

			CreateFolder(fname);
			if (File.Exists(fname))
			{
				File.Delete(fname);
			}

			var fs = new FileStream(fname, FileMode.Create, FileAccess.Write, FileShare.None);
			fs.SetLength(fileSize);
			_openedFiles.Add(id, fs);
			return id;
		}

		public void WriteFilePartially(long fileId, byte[] content, int offset, int length)
		{
			var stream = _openedFiles[fileId];
			stream.Position = offset;
			stream.Write(content, 0, length);
		}

		public void WriteFile(long appId, Environment.SpecialFolder folder, string fileName, byte[] content)
		{
			var app = _pendingInstallations[appId];
			var path = Resolve(app, folder, fileName);
			File.WriteAllBytes(path, content);
		}

		public void DeleteFile(long appId, Environment.SpecialFolder folder, string fileName)
		{
			var app = _installedApplications[appId];
			var path = Resolve(app, folder, fileName);
			File.Delete(path);
		}

		private static void CreateFolder(string fname)
		{
			var dir = Path.GetDirectoryName(fname);
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
			var folderPath = Environment.GetFolderPath(folder);
			var fullPath = Path.Combine(folderPath, appFolder, relativeFileName);
			return fullPath;
		}

		#endregion

		public void Dispose()
		{
			_cancellationTokenSource.Cancel();
			_task.Wait();

			lock (_syncRoot)
			{
				foreach (var proc in _processes.Values)
				{
					proc.TryKill();
				}
				_processes.Clear();
			}
		}
	}
}