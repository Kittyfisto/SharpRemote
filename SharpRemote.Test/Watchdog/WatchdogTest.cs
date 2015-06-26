using System;
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
		[Test]
		[Description("Verifies that deploying sharpremote via a watchdog works")]
		public void TestInstallAndExecuteSharpRemote()
		{
			Process process;
			using (var remote = new RemoteWatchdog())
			{
				var watchdog = new SharpRemote.Watchdog.Watchdog(remote);

				var files = new[]
				{
					"log4net.dll",
					"SharpRemote.dll",
					"SampleBrowser.exe"
				};
				var folder = Path.GetDirectoryName(Uri.UnescapeDataString(new UriBuilder(typeof(RemoteWatchdog).Assembly.CodeBase).Path));

				var desc = new ApplicationDescriptor
				{
					Name = "SharpRemote",
					FolderName = "SharpRemote",
				};
				InstalledApplication app;
				using (var installer = watchdog.StartInstallation(desc))
				{
					var fileNames = files.Select(x => Path.Combine(folder, x)).ToList();
					installer.AddFiles(fileNames, Environment.SpecialFolder.LocalApplicationData);
					app = installer.Commit();
				}

				app.Should().NotBeNull();

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
				var procs = Process.GetProcessesByName("SampleBrowser");
				procs.Count().Should().Be(1);
				process = procs[0];
				process.HasExited.Should().BeFalse();
			}

			// After having been disposed of, the process should no longer be running...
			process.HasExited.Should().BeTrue();
		}
	}
}