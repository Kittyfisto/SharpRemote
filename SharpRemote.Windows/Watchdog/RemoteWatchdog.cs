using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;

namespace SharpRemote.Watchdog
{
	internal sealed class RemoteWatchdog
		: IRemoteWatchdog
	{
		private long _nextFileId;
		private long _nextAppId;
		private readonly Dictionary<long, Stream> _openedFiles;
		private readonly object _syncRoot;
		private readonly Dictionary<long, InstalledApplication> _pendingInstallations;
		private readonly Dictionary<long, InstalledApplication> _installedApplications;

		public RemoteWatchdog()
		{
			_syncRoot = new object();
			_openedFiles = new Dictionary<long, Stream>();
			_pendingInstallations = new Dictionary<long, InstalledApplication>();
			_installedApplications = new Dictionary<long, InstalledApplication>();
		}

		#region Installation

		public long StartApplicationInstallation(ApplicationDescriptor description)
		{
			lock (_syncRoot)
			{
				var id = Interlocked.Increment(ref _nextAppId);
				_pendingInstallations.Add(id, new InstalledApplication(id, description));
				return id;
			}
		}

		public InstalledApplication CommitApplicationInstallation(long appId)
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

		public void AbortApplicationInstallation(long appId)
		{
			throw new NotImplementedException();
		}

		public void RemoveApplication()
		{
			throw new NotImplementedException();
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
	}
}