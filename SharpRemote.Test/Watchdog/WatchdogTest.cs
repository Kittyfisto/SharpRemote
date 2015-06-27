using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Watchdog;

namespace SharpRemote.Test.Watchdog
{
	[TestFixture]
	public sealed class WatchdogTest
	{
		private static readonly string SharpRemoteFolder =
			Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(typeof (RemoteWatchdog).Assembly.CodeBase).Path));
		private static readonly string[] SharpRemoteFiles = new[]
				{
					"log4net.dll",
					"SharpRemote.dll",
					"SampleBrowser.exe"
				};

		private InProcessRemotingSilo _silo;
		private RemoteWatchdog _watchdog;

		private void DeploySharpRemote(IApplicationInstaller installer)
		{
			List<string> fileNames = SharpRemoteFiles.Select(x => Path.Combine(SharpRemoteFolder, x)).ToList();
			installer.AddFiles(fileNames, Environment.SpecialFolder.LocalApplicationData);
		}

		[SetUp]
		public void SetUp()
		{
			_silo = new InProcessRemotingSilo();
		}

		private IRemoteWatchdog CreateWatchdog()
		{
			return _silo.CreateGrain<IRemoteWatchdog>(typeof(RemoteWatchdog));
			//return _watchdog = new RemoteWatchdog();
		}

		[TearDown]
		public void TearDown()
		{
			//_watchdog.Dispose();
			_silo.Dispose();
		}

		private ApplicationDescriptor SharpRemote(string version)
		{
			return new ApplicationDescriptor
			{
				Name = string.Format("SharpRemote {0}", version),
				FolderName = string.Format("SharpRemote {0}", version),
			};
		}

		private ApplicationInstanceDescription CreateBrowserInstance(InstalledApplication app)
		{
			return new ApplicationInstanceDescription
			{
				AppId = app.Id,
				Executable = app.Files.First(x => x.Filename.EndsWith("SampleBrowser.exe")),
				Name = "Test Host"
			};
		}

		private void VerifyPostSharpDeployment(InstalledApplication app)
		{
			string sourceFolder =
				Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(typeof(RemoteWatchdog).Assembly.CodeBase).Path));

			foreach (var file in SharpRemoteFiles)
			{
				var sourceFileName = Path.Combine(sourceFolder, file);
				var destFileName = RemoteWatchdog.Resolve(app, Environment.SpecialFolder.LocalApplicationData, file);

				ApplicationInstallerTest.FilesAreEqual(sourceFileName, destFileName)
				                        .Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that deploying sharpremote via a watchdog works")]
		public void TestInstallAndExecuteSharpRemote()
		{
			var watchdog = new SharpRemote.Watchdog.Watchdog(CreateWatchdog());

			InstalledApplication app;
			using (IApplicationInstaller installer = watchdog.StartInstallation(SharpRemote("0.1")))
			{
				DeploySharpRemote(installer);
				app = installer.Commit();
			}

			app.Should().NotBeNull();

			// Let's verify that the deployment actually worked...
			VerifyPostSharpDeployment(app);

			// Now that SharpRemote is deployed we can start an actual instance...
			var instance = new ApplicationInstanceDescription
			{
				AppId = app.Id,
				Executable = app.Files.First(x => x.Filename.EndsWith("SampleBrowser.exe")),
				Name = "Test Host"
			};
			watchdog.RegisterApplicationInstance(instance);

			// Due to the watchdog being executed on the same computer, we expect
			// the process to be running now...
			IsBrowserRunning().Should().BeTrue();
		}

		private bool IsBrowserRunning()
		{
			Process process;
			return IsBrowserRunning(out process);
		}

		private bool IsBrowserRunning(out Process process)
		{
			Process[] procs = Process.GetProcessesByName("SampleBrowser");
			if (procs.Length != 1)
			{
				process = null;
				return false;
			}

			process = procs[0];
			return !process.HasExited;
		}

		[Test]
		[Description("Verifies that deploying two different applications at the same time works")]
		public void TestInstallConcurrently1()
		{
			var watchdog = new SharpRemote.Watchdog.Watchdog(CreateWatchdog());

			InstalledApplication app1, app2;
			using (IApplicationInstaller installer1 = watchdog.StartInstallation(SharpRemote("0.2")))
			using (IApplicationInstaller installer2 = watchdog.StartInstallation(SharpRemote("0.3")))
			{
				DeploySharpRemote(installer2);
				DeploySharpRemote(installer1);
				app2 = installer2.Commit();
				app1 = installer1.Commit();
			}

			app2.Should().NotBeNull();
			app1.Should().NotBeNull();

			// Let's verify that the deployment actually worked...
			VerifyPostSharpDeployment(app1);
			VerifyPostSharpDeployment(app2);
		}

		[Test]
		[Description("Verifies that deploying the same application twice at the same time is not allowed")]
		public void TestInstallConcurrently2()
		{
			var watchdog = new SharpRemote.Watchdog.Watchdog(CreateWatchdog());

			InstalledApplication app1;
			var desc = SharpRemote("0.4");
			using (IApplicationInstaller installer1 = watchdog.StartInstallation(desc))
			{
				DeploySharpRemote(installer1);

				new Action(() => watchdog.StartInstallation(desc))
					.ShouldThrow<InstallationFailedException>()
					.WithMessage("There already is a pending installation for the same application - this installation must be completed or aborted in order for a new installation to be allowed");

				app1 = installer1.Commit();
			}

			app1.Should().NotBeNull();

			// Let's verify that the deployment actually worked...
			VerifyPostSharpDeployment(app1);
		}

		[Test]
		[Description("Verifies that an update can install completely new files while not touching old ones")]
		public void TestColdUpdate1()
		{
			var watchdog = new SharpRemote.Watchdog.Watchdog(CreateWatchdog());

			InstalledApplication app, update;
			var desc = SharpRemote("0.5");
			using (IApplicationInstaller installer = watchdog.StartInstallation(desc))
			{
				DeploySharpRemote(installer);
				app = installer.Commit();
			}

			// Let's try patching the pdb...
			using (var installer = watchdog.StartInstallation(desc, Installation.ColdUpdate))
			{
				var pdb = Path.Combine(SharpRemoteFolder, "SharpRemote.pdb");
				installer.AddFile(pdb, Environment.SpecialFolder.LocalApplicationData);
				update = installer.Commit();
			}

			// The update should consists of all files from the first installation *AND* the pdb
			// we installed as an update
			update.Files.Count.Should().Be(app.Files.Count + 1);
			var updated = update.Files.Except(app.Files).ToList();
			updated.Count.Should().Be(1);
			updated[0].Filename.Should().Be("SharpRemote.pdb");
			updated[0].Folder.Should().Be(Environment.SpecialFolder.LocalApplicationData);
			updated[0].Id.Should().Be(4);
		}

		[Test]
		[Description("Verifies that an update can be installed even its files are in used")]
		public void TestColdUpdate2()
		{
			var watchdog = new SharpRemote.Watchdog.Watchdog(CreateWatchdog());

			InstalledApplication app, update;
			var desc = SharpRemote("0.6");
			using (IApplicationInstaller installer = watchdog.StartInstallation(desc))
			{
				DeploySharpRemote(installer);
				app = installer.Commit();
			}

			// Let's start a browser application to ensure that some files from the update are now in use...
			var instance = CreateBrowserInstance(app);
			watchdog.RegisterApplicationInstance(instance);
			IsBrowserRunning().Should().BeTrue();

			// Performing a cold update should be possible because it kills the app(s) first..
			using (var installer = watchdog.StartInstallation(desc, Installation.ColdUpdate))
			{
				IsBrowserRunning().Should().BeFalse("because the update needed to kill the browser");

				var pdb = Path.Combine(SharpRemoteFolder, "SharpRemote.dll");
				installer.AddFile(pdb, Environment.SpecialFolder.LocalApplicationData);
				var browser = Path.Combine(SharpRemoteFolder, "SampleBrowser.exe");
				installer.AddFile(browser, Environment.SpecialFolder.LocalApplicationData);
				update = installer.Commit();

				IsBrowserRunning().Should().BeTrue("because after the update's finished all application instances should be running again");
			}

			// The update shouldn't have written new files, not even their file sizes should've changed...
			app.Files.Should().BeEquivalentTo(update.Files);
		}

		[Test]
		[Description("Verifies that a hot update of a file in use is not possible")]
		public void TestHotUpdate1()
		{
			var watchdog = new SharpRemote.Watchdog.Watchdog(CreateWatchdog());

			InstalledApplication app;
			var desc = SharpRemote("0.7");
			using (IApplicationInstaller installer = watchdog.StartInstallation(desc))
			{
				DeploySharpRemote(installer);
				app = installer.Commit();
			}

			// Let's start a browser application to ensure that some files from the update are now in use...
			var instance = CreateBrowserInstance(app);
			watchdog.RegisterApplicationInstance(instance);
			IsBrowserRunning().Should().BeTrue();

			// Performing a cold update should be possible because it kills the app(s) first..
			using (var installer = watchdog.StartInstallation(desc, Installation.HotUpdate))
			{
				IsBrowserRunning().Should().BeTrue("because the update shouldn't kill any instance");

				var browser = Path.Combine(SharpRemoteFolder, "SampleBrowser.exe");
				installer.AddFile(browser, Environment.SpecialFolder.LocalApplicationData);
				new Action(() => installer.Commit())
					.ShouldThrow<InstallationFailedException>()
					.WithMessage("Application of 'SharpRemote 0.7' failed")
					.WithInnerException<UnauthorizedAccessException>();
			}
		}
	}
}