using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SharpRemote.Watchdog
{
	internal sealed class ApplicationInstaller
		: IApplicationInstaller
	{
		private const int BlockSize = 4096;
		private readonly long _appId;

		private readonly byte[] _blockBuffer;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly CancellationTokenSource _commitTokenSource;
		private readonly ApplicationDescriptor _descriptor;
		private readonly ConcurrentBag<File> _pendingFiles;
		private readonly Task _task;
		private readonly IRemoteWatchdog _watchdog;
		private float _progress;
		private long _totalSize;
		private long _transferedSize;
		private Exception _exception;
		private bool _success;

		public ApplicationInstaller(IRemoteWatchdog watchdog, ApplicationDescriptor descriptor)
		{
			if (watchdog == null) throw new ArgumentNullException("watchdog");

			_blockBuffer = new byte[BlockSize];
			_descriptor = descriptor;
			_watchdog = watchdog;
			_pendingFiles = new ConcurrentBag<File>();
			_cancellationTokenSource = new CancellationTokenSource();
			_commitTokenSource = new CancellationTokenSource();
			_task = Task.Factory.StartNew(InstallApplication);

			_appId = _watchdog.StartInstallation(descriptor);
		}

		public void Dispose()
		{
			if (!_success)
			{
				try
				{
					_watchdog.AbortInstallation(_appId);
				}
				catch (Exception)
				{
				}
			}

			_cancellationTokenSource.Cancel();
			_task.Wait();

			File pendingFile;
			while (_pendingFiles.TryTake(out pendingFile))
			{
				pendingFile.Dispose();
			}
		}

		public double Progress
		{
			get { return _progress; }
		}

		public void AddFile(string sourceFileName, Environment.SpecialFolder destinationFolder, string destinationPath = null)
		{
			ThrowIfNecessary();
			var file = new File(sourceFileName, destinationFolder, destinationPath);
			_totalSize += file.FileSize;
			_pendingFiles.Add(file);
		}

		public void AddFiles(string sourceFolder, Environment.SpecialFolder destinationFolder, string destinationPath = null)
		{
			var files = Directory.GetFiles(sourceFolder);
			foreach (var file in files)
			{
				AddFile(file, destinationFolder, destinationPath);
			}
		}

		public void AddFiles(List<string> files, Environment.SpecialFolder destinationFolder, string destinationPath = null)
		{
			foreach (var file in files)
			{
				AddFile(file, destinationFolder, destinationPath);
			}
		}

		public InstalledApplication Commit()
		{
			_commitTokenSource.Cancel();
			_task.Wait();

			ThrowIfNecessary();

			// No exception was thrown - let's try to end the installation
			// on the remote system - if that work's then we're done
			var ret = _watchdog.CommitInstallation(_appId);
			_success = true;
			return ret;
		}

		private void ThrowIfNecessary()
		{
			if (_exception != null)
				throw new InstallationFailedException(string.Format("Application of '{0}' failed", _descriptor.Name), _exception);
		}

		private void InstallApplication()
		{
			try
			{
				CancellationToken token = _cancellationTokenSource.Token;
				while (!token.IsCancellationRequested)
				{
					File nextFile;
					if (_pendingFiles.TryTake(out nextFile))
					{
						using (nextFile)
						{
							TransferFile(nextFile);
						}
					}
					else
					{
						if (_commitTokenSource.IsCancellationRequested)
							break; //< Installation finished

						// TODO: Use wait handle to eliminate sleeps...
						Thread.Sleep(10);
					}
				}
			}
			catch (Exception e)
			{
				_exception = e;
			}
		}

		private void TransferFile(File nextFile)
		{
			long fileId = _watchdog.CreateFile(_appId, nextFile.DestinationFolder, nextFile.RelativeFileName, nextFile.FileSize);

			int offset = 0;
			for (int i = 0; i < nextFile.NumBlocks; ++i)
			{
				long left = nextFile.FileSize - offset;
				var toRead = (int) Math.Min(left, _blockBuffer.Length);
				int read = nextFile.Stream.Read(_blockBuffer, 0, toRead);

				_watchdog.WriteFilePartially(fileId, _blockBuffer, offset, toRead);
				offset += read;
				_transferedSize += read;
				_progress = (float) (1.0*_transferedSize/_totalSize);
			}
		}

		private struct File
			: IDisposable
		{
			public readonly Environment.SpecialFolder DestinationFolder;
			public readonly long FileSize;
			public readonly int NumBlocks;
			public readonly string RelativeFileName;
			public readonly Stream Stream;

			public File(string sourceFileName, Environment.SpecialFolder destinationFolder, string destinationPath)
			{
				DestinationFolder = destinationFolder;

				var info = new FileInfo(sourceFileName);

				RelativeFileName = destinationPath != null
					                   ? Path.Combine(destinationPath, info.Name)
					                   : info.Name;

				FileSize = info.Length;
				NumBlocks = (int) Math.Ceiling(1.0*FileSize/BlockSize);
				Stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
			}

			public void Dispose()
			{
				Stream.Dispose();
			}
		}
	}
}