using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class ProcessWatchdogTest
	{
		[Test]
		public void TestDispose1()
		{
			var watchdog = new ProcessWatchdog();
			new Action(watchdog.Dispose).ShouldNotThrow();
		}

		[Test]
		public void TestDispose2()
		{
			ProcessWatchdog watchdog;
			using (watchdog = new ProcessWatchdog())
			{
				watchdog.RemotePort.Should().NotHaveValue();
				watchdog.HostedProcessId.Should().NotHaveValue();

				watchdog.Start();
				watchdog.RemotePort.Should().HaveValue();
				watchdog.HostedProcessId.Should().HaveValue();
			}

			watchdog.RemotePort.Should().NotHaveValue();
			watchdog.HostedProcessId.Should().NotHaveValue();
		}
	}
}