using System;
using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Watchdog;

namespace SharpRemote.Test.Watchdog
{
	[TestFixture]
	public sealed class ApplicationInstallerTest
	{
		private string _sharpRemoteLibraryLocation;
		private string _binFolder;
		private InProcessRemotingSilo _silo;
		private IRemoteWatchdog _watchdog;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var assembly = typeof (RemoteWatchdog).Assembly;
			_sharpRemoteLibraryLocation = assembly.Location;
			_binFolder = Path.GetDirectoryName(_sharpRemoteLibraryLocation);
		}

		[SetUp]
		public void SetUp()
		{
			//_silo = new InProcessRemotingSilo();
			//_watchdog = _silo.CreateGrain<IRemoteWatchdog>(typeof (RemoteWatchdog));
			_watchdog = new RemoteWatchdog();
		}

		[TearDown]
		public void TearDown()
		{
			//_silo.Dispose();
		}

		[Test]
		[Description("Verifies that an application with multiple files in the same directory works")]
		public void TestInstallMultipleFiles()
		{
			var descriptor = new ApplicationDescriptor
			{
				Name = "TestInstallSingleFile",
				FolderName = "TestInstallSingleFile",
			};

			InstalledApplication app;
			using (var installer = new ApplicationInstaller(_watchdog, descriptor))
			{
				installer.AddFiles(_binFolder, Environment.SpecialFolder.CommonDocuments);
				app = installer.Commit();
			}

			var expectedFiles = Directory.GetFiles(_binFolder);
			var actualFiles = app.Files;

			actualFiles.Count.Should().Be(expectedFiles.Length);
			for (int i = 0; i < expectedFiles.Length; ++i)
			{
				var fullPath = RemoteWatchdog.Resolve(descriptor.FolderName, Environment.SpecialFolder.CommonDocuments,
														 actualFiles[i].Filename);
				FilesAreEqual(new FileInfo(expectedFiles[i]), new FileInfo(fullPath));
			}
		}

		[Test]
		[Description("Verifies that an application with a single file works")]
		public void TestInstallSingleFile()
		{
			var descriptor = new ApplicationDescriptor
			{
				Name = "TestInstallSingleFile",
				FolderName = "TestInstallSingleFile",
			};
			var fullPath = RemoteWatchdog.Resolve(descriptor.FolderName, Environment.SpecialFolder.CommonApplicationData,
													 "SharpRemote.dll");
			var original = new FileInfo(_sharpRemoteLibraryLocation);

			if (File.Exists(fullPath))
				File.Delete(fullPath);

			InstalledApplication app;
			using (var installer = new ApplicationInstaller(_watchdog, descriptor))
			{
				installer.AddFile(_sharpRemoteLibraryLocation, Environment.SpecialFolder.CommonApplicationData);
				app = installer.Commit();
			}

			app.Id.Should().Be(1);
			app.Descriptor.Should().Be(descriptor);
			app.Files.Count.Should().Be(1);
			var file = app.Files[0];
			file.Id.Should().Be(1);
			file.Folder.Should().Be(Environment.SpecialFolder.CommonApplicationData);
			file.Filename.Should().Be("SharpRemote.dll");
			file.FileLength.Should().Be(original.Length);

			var copy = new FileInfo(fullPath);
			copy.Exists.Should().BeTrue("Because the file should've been created during the installation");
			copy.Length.Should().Be(file.FileLength);

			FilesAreEqual(original, copy).Should().BeTrue();
		}

		public static bool FilesAreEqual(string first, string second)
		{
			return FilesAreEqual(new FileInfo(first), new FileInfo(second));
		}

		public static bool FilesAreEqual(FileInfo first, FileInfo second)
		{
			if (first.Length != second.Length)
				return false;

			const int blockSize = 4096;
			int iterations = (int)Math.Ceiling((double)first.Length / blockSize);

			using (FileStream fs1 = first.OpenRead())
			using (FileStream fs2 = File.Open(second.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var one = new byte[blockSize];
				var two = new byte[blockSize];

				for (int i = 0; i < iterations; i++)
				{
					fs1.Read(one, 0, blockSize);
					fs2.Read(two, 0, blockSize);

					for (int x = 0; x < blockSize; ++x)
					{
						if (one[x] != two[x])
							return false;
					}
				}
			}

			return true;
		}
	}
}