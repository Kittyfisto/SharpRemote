using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Watchdog;

namespace SharpRemote.Test.Watchdog
{
	[TestFixture]
	public sealed class WatchdogTest
	{
		private static readonly string[] SharpRemoteFiles = new[]
				{
					"log4net.dll",
					"SharpRemote.dll",
					"SampleBrowser.exe"
				};
		private void DeploySharpRemote(IApplicationInstaller installer)
		{
			string folder =
				Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(typeof (RemoteWatchdog).Assembly.CodeBase).Path));
			List<string> fileNames = SharpRemoteFiles.Select(x => Path.Combine(folder, x)).ToList();
			installer.AddFiles(fileNames, Environment.SpecialFolder.LocalApplicationData);
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
			Process process;
			using (var remote = new RemoteWatchdog())
			{
				var watchdog = new SharpRemote.Watchdog.Watchdog(remote);

				var desc = new ApplicationDescriptor
					{
						Name = "SharpRemote",
						FolderName = "SharpRemote",
					};
				InstalledApplication app;
				using (IApplicationInstaller installer = watchdog.StartInstallation(desc))
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
				Process[] procs = Process.GetProcessesByName("SampleBrowser");
				procs.Count().Should().Be(1);
				process = procs[0];
				process.HasExited.Should().BeFalse();
			}

			// After having been disposed of, the process should no longer be running...
			process.HasExited.Should().BeTrue();
		}

		[Test]
		[Description("Verifies that deploying two different applications at the same time works")]
		public void TestInstallConcurrently1()
		{
			using (var remote = new RemoteWatchdog())
			{
				var watchdog = new SharpRemote.Watchdog.Watchdog(remote);

				var desc1 = new ApplicationDescriptor
					{
						Name = "SharpRemote 0.1",
						FolderName = "SharpRemote 0.1",
					};
				var desc2 = new ApplicationDescriptor
					{
						Name = "SharpRemote 0.2",
						FolderName = "SharpRemote 0.2",
					};

				InstalledApplication app1, app2;
				using (IApplicationInstaller installer1 = watchdog.StartInstallation(desc1))
				using (IApplicationInstaller installer2 = watchdog.StartInstallation(desc2))
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
		}

		[Test]
		[Description("Verifies that deploying the same application twice at the same time is not allowed")]
		public void TestInstallConcurrently2()
		{
			using (var remote = new RemoteWatchdog())
			{
				var watchdog = new SharpRemote.Watchdog.Watchdog(remote);

				var desc1 = new ApplicationDescriptor
				{
					Name = "SharpRemote 0.3",
					FolderName = "SharpRemote 0.3",
				};
				var desc2 = new ApplicationDescriptor
				{
					Name = "SharpRemote 0.3",
					FolderName = "SharpRemote 0.3",
				};

				InstalledApplication app1;
				using (IApplicationInstaller installer1 = watchdog.StartInstallation(desc1))
				{
					DeploySharpRemote(installer1);

					new Action(() => watchdog.StartInstallation(desc2))
						.ShouldThrow<InstallationFailedException>()
						.WithMessage("There already is a pending installation for the same application - this installation must be completed or aborted in order for a new installation to be allowed");

					app1 = installer1.Commit();
				}

				app1.Should().NotBeNull();

				// Let's verify that the deployment actually worked...
				VerifyPostSharpDeployment(app1);
			}
		}
	}
}