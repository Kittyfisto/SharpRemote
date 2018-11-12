using System;
using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class ProcessWatchdogTest
	{
		[Test]
		[Description("Verifies that starting a process again immediately after having been killed works")]
		public void TestStartKillStart()
		{
			using (var watchdog = new ProcessWatchdog())
			{
				watchdog.Start();
				watchdog.IsProcessRunning.Should().BeTrue();
				watchdog.HasProcessFailed.Should().BeFalse();

				var pid = watchdog.HostedProcessId.Value;
				var proc = Process.GetProcessById(pid);
				proc.Kill();

				watchdog.Start();
				watchdog.IsProcessRunning.Should().BeTrue("Because we've just started that process again");
				watchdog.HasProcessFailed.Should().BeFalse("Because we've just started that process again");
			}
		}

		[Test]
		public void TestStartKill()
		{
			using (var watchdog = new ProcessWatchdog())
			{
				watchdog.Start();
				watchdog.IsProcessRunning.Should().BeTrue();
				watchdog.HasProcessFailed.Should().BeFalse();

				var pid = watchdog.HostedProcessId.Value;
				var proc = Process.GetProcessById(pid);
				proc.Kill();

				watchdog.Property(x => x.HasProcessFailed).ShouldEventually().BeTrue();
				watchdog.Property(x => x.IsProcessRunning).ShouldEventually().BeFalse();
				watchdog.Property(x => x.ProcessFailureReason).ShouldEventually().Be(ProcessFailureReason.HostProcessExitedUnexpectedly);
			}
		}

		[Test]
		[Description("Verifies that after the process has been killed, the watchdog no longer reports the host process as alive - nor its current port")]
		public void TestTryKill()
		{
			using (var watchdog = new ProcessWatchdog())
			{
				watchdog.Start();

				watchdog.RemotePort.Should().HaveValue();
				watchdog.IsProcessRunning.Should().BeTrue();
				watchdog.HasProcessFailed.Should().BeFalse();
				watchdog.ProcessFailureReason.Should().BeNull("because the process shouldn't have failed");

				watchdog.TryKill();
				watchdog.RemotePort.Should().NotHaveValue();
				watchdog.IsProcessRunning.Should().BeFalse();
				watchdog.HasProcessFailed.Should().BeTrue();
				watchdog.ProcessFailureReason.Should()
					.BeNull("because we've explicitly killed the process and thus no fault occured");
			}
		}

		[Test]
		[Description("Verifies that the state exposed by ProcessWatchdog is reset upon a call to Start()")]
		public void TestStartKillStartAgain()
		{
			using (var watchdog = new ProcessWatchdog())
			{
				watchdog.Start();

				const string reason = "because the process shouldn't have failed";
				watchdog.IsProcessRunning.Should().BeTrue(reason);
				watchdog.HasProcessFailed.Should().BeFalse(reason);
				watchdog.HostedProcessState.Should().Be(HostState.Ready, reason);
				watchdog.ProcessFailureReason.Should().BeNull(reason);


				var pid = watchdog.HostedProcessId.Value;
				var proc = Process.GetProcessById(pid);
				proc.Kill();

				const string failureReason =
					"because we've killed the process and that failure should've been detected";
				watchdog.Property(x => x.IsProcessRunning).ShouldEventually().BeFalse(failureReason);
				watchdog.Property(x => x.HasProcessFailed).ShouldEventually().BeTrue(failureReason);
				watchdog.Property(x => x.HostedProcessState).ShouldEventually().Be(HostState.Dead, failureReason);
				watchdog.Property(x => x.ProcessFailureReason).ShouldEventually().Be(ProcessFailureReason.HostProcessExitedUnexpectedly, failureReason);


				watchdog.Start();
				const string newReason = "because we've restarted the process and thus everything should be back to normal again";
				watchdog.IsProcessRunning.Should().BeTrue(newReason);
				watchdog.HasProcessFailed.Should().BeFalse(newReason);
				watchdog.HostedProcessState.Should().Be(HostState.Ready, newReason);
				watchdog.ProcessFailureReason.Should().BeNull(newReason);
			}
		}

		[Test]
		public void TestDispose1()
		{
			var watchdog = new ProcessWatchdog();
			watchdog.IsDisposed.Should().BeFalse();

			new Action(watchdog.Dispose).ShouldNotThrow();
			watchdog.IsDisposed.Should().BeTrue();
		}

		[Test]
		public void TestDispose2()
		{
			ProcessWatchdog watchdog;
			int pid;
			using (watchdog = new ProcessWatchdog())
			{
				watchdog.RemotePort.Should().NotHaveValue();
				watchdog.HostedProcessId.Should().NotHaveValue();

				watchdog.Start();
				watchdog.RemotePort.Should().HaveValue();
				watchdog.HostedProcessId.Should().HaveValue();

				pid = watchdog.HostedProcessId.Value;
				ProcessWithPidShouldBeRunning(pid);
			}

			watchdog.RemotePort.Should().NotHaveValue();
			watchdog.HostedProcessId.Should().NotHaveValue();
			ProcessWithPidShouldNotBeRunning(pid);
		}

		private void ProcessWithPidShouldNotBeRunning(int pid)
		{
			if (IsProcessRunning(pid))
			{
				Assert.Fail("Expected process with PID {0} to no longer be running, but it's still alive!");
			}
		}

		private static void ProcessWithPidShouldBeRunning(int pid)
		{
			if (!IsProcessRunning(pid))
			{
				Assert.Fail("Expected a process with PID {0} to be running, but it's not!");
			}
		}

		private static bool IsProcessRunning(int pid)
		{
			try
			{
				using (Process.GetProcessById(pid))
				{
					return true;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}