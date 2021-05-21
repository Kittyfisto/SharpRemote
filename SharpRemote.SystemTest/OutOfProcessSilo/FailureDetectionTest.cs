using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using log4net.Core;
using Moq;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.SystemTest.OutOfProcessSilo
{
	[TestFixture]
	public sealed class FailureDetectionTest
		: AbstractTest
	{
		[Test]
		[Repeat(10)]
		[LocalTest("Won't run on AppVeyor 100% of the time")]
		[Description("Verifies that a crash of the host process is detected when it happens while a method call")]
		public void TestFailureDetection1()
		{
			Failure? failure = null;
			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.OnFailure(It.IsAny<Failure>()))
			       .Callback((Failure x) => failure = x);

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: handler.Object))
			{
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof (KillsProcess));
				new Action(proxy.Do)
					.Should().Throw<ConnectionLostException>(
						"Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				WaitFor(() => silo.HasProcessFailed, TimeSpan.FromSeconds(1))
					.Should()
					.BeTrue(
						"Because an aborted thread that is currently invoking a remote method call should cause SharpRemote to kill the host process and report failure");
				silo.IsProcessRunning.Should().BeFalse();

				WaitFor(() => failure != null, TimeSpan.FromSeconds(1))
					.Should().BeTrue("Because the IFailureHandler should've been notified in time");

				(failure == Failure.ConnectionFailure ||
				 failure == Failure.HostProcessExited).Should().BeTrue();
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("Timing dependant tests won't run on AppVeyor")]
		[Description("Verifies that death of the host process can be detected, even if the silo isn't actively used")]
		public void TestFailureDetection10()
		{
			using (var handle1 = new ManualResetEvent(false))
			using (var handle2 = new ManualResetEvent(false))
			{
				Failure? failure1 = null;
				Failure? failure2 = null;
				Resolution? resolution = null;

				var handler = new Mock<IFailureHandler>();
				handler.Setup(x => x.OnFailure(It.IsAny<Failure>()))
				       .Callback((Failure f) =>
					       {
						       failure1 = f;
						       handle1.Set();
					       });
				handler.Setup(x => x.OnResolutionFinished(It.IsAny<Failure>(), It.IsAny<Decision>(), It.IsAny<Resolution>()))
				       .Callback((Failure f, Decision d, Resolution r) =>
					       {
						       failure2 = f;
						       resolution = r;
						       handle2.Set();
					       });

				var settings = new FailureSettings
					{
						HeartbeatSettings =
						{
							ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
							Interval = TimeSpan.FromMilliseconds(100),
							SkippedHeartbeatThreshold = 4
						}
					};

				using (var log = new LogCollector("SharpRemote", Level.Info, Level.Warn, Level.Error))
				using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: settings, failureHandler: handler.Object))
				{
					silo.Start();
					int? pid = silo.HostProcessId;
					pid.Should().HaveValue();

					Process hostProcess = Process.GetProcessById(pid.Value);
					hostProcess.Kill();

					WaitHandle.WaitAll(new[] {handle1, handle2}, TimeSpan.FromSeconds(2))
					          .Should().BeTrue("Because the failure should've been detected as well as handled");

					failure1.Should().Be(Failure.HostProcessExited);
					failure2.Should().Be(failure1);
					resolution.Should().Be(Resolution.Stopped);

					var expectedMessage = string.Format("Host '{0}' (PID: {1}) exited unexpectedly with error code -1",
						ProcessWatchdog.SharpRemoteHost,
						pid.Value);
					log.Events.Should().Contain(x => x.Level == Level.Error &&
					                                 x.RenderedMessage.Contains(expectedMessage));
				}
			}
		}

		[Test]
		[Description("Verifies that the silo can be disposed of from within the FaultHandled event")]
		public void TestFailureDetection11()
		{
			var handler = new Mock<IFailureHandler>();

			var settings = new FailureSettings
				{
					HeartbeatSettings =
					{
						ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
						Interval = TimeSpan.FromMilliseconds(100),
						SkippedHeartbeatThreshold = 4
					}
				};

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: settings, failureHandler: handler.Object))
			using (var handle = new ManualResetEvent(false))
			{
				handler.Setup(x => x.OnResolutionFinished(It.IsAny<Failure>(), It.IsAny<Decision>(), It.IsAny<Resolution>()))
				       .Callback((Failure f, Decision d, Resolution r) =>
					       {
						       silo.Dispose();
						       handle.Set();
					       });

				silo.Start();
				int? id = silo.HostProcessId;
				id.Should().HaveValue();

				Process hostProcess = Process.GetProcessById(id.Value);
				hostProcess.Kill();

				handle.WaitOne(TimeSpan.FromSeconds(2))
				      .Should().BeTrue("Because the failure should've been detected as well as handled");

				silo.IsDisposed.Should().BeTrue();
				silo.HasProcessFailed.Should().BeTrue();
				silo.IsProcessRunning.Should().BeFalse();
			}
		}

		[Test]
		[Description(
			"Verifies that an abortion of the executing thread of a remote method invocation is detected and that it causes a connection loss"
			)]
		public void TestFailureDetection2()
		{
			Failure? failure = null;
			Decision? decision = null;
			Resolution? resolution = null;

			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.OnResolutionFinished(It.IsAny<Failure>(), It.IsAny<Decision>(), It.IsAny<Resolution>()))
			       .Callback((Failure f, Decision d, Resolution r) =>
				       {
					       failure = f;
					       decision = d;
					       resolution = r;
				       });

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: handler.Object))
			{
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof (AbortsThread));
				new Action(proxy.Do)
					.Should().Throw<ConnectionLostException>(
						"Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				WaitFor(() => silo.HasProcessFailed, TimeSpan.FromSeconds(1))
					.Should().BeTrue("Because an unexpected exit of the host process counts as a failure");
				silo.IsProcessRunning.Should().BeFalse();

				WaitFor(() => resolution != null, TimeSpan.FromSeconds(1)).Should().BeTrue();
				(failure == Failure.ConnectionFailure ||
				 failure == Failure.HostProcessExited).Should().BeTrue("because we expected either a ConnectionFailure or HostProcessExited, but found: {0}", failure);
				resolution.Should().Be(Resolution.Stopped);
			}
		}

		[Test]
		[Repeat(10)]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that a complete deadlock of the important remoting threads is detected")]
		public void TestFailureDetection3()
		{
			Failure? failure1 = null;
			Failure? failure2 = null;
			Decision? decision = null;
			Resolution? resolution = null;

			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.OnFailure(It.IsAny<Failure>()))
			       .Callback((Failure f) => failure1 = f);
			handler.Setup(x => x.OnResolutionFinished(It.IsAny<Failure>(), It.IsAny<Decision>(), It.IsAny<Resolution>()))
			       .Callback((Failure f, Decision d, Resolution r) =>
				       {
					       failure2 = f;
					       decision = d;
					       resolution = r;
				       });

			var settings = new FailureSettings
				{
					HeartbeatSettings =
					{
						ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
						Interval = TimeSpan.FromMilliseconds(100),
						SkippedHeartbeatThreshold = 4
					}
				};
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: settings, failureHandler: handler.Object))
			{
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof (DeadlocksProcess));
				new Action(() =>
					{
						Task.Factory.StartNew(proxy.Do, TaskCreationOptions.LongRunning)
						    .Wait(TimeSpan.FromSeconds(10))
						    .Should().BeTrue("Because the silo should've detected the deadlock in time");
					})
					.Should().Throw<ConnectionLostException>(
						"Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				WaitFor(() => silo.HasProcessFailed, TimeSpan.FromSeconds(1))
					.Should()
					.BeTrue("Because the heartbeat mechanism should have detected that the endpoint doesn't respond anymore");
				WaitFor(() => failure1 != null, TimeSpan.FromSeconds(1)).Should().BeTrue();
				WaitFor(() => failure2 != null, TimeSpan.FromSeconds(1)).Should().BeTrue();

				silo.IsProcessRunning.Should().BeFalse();
				failure1.Should().Be(Failure.HeartbeatFailure);
				failure2.Should().Be(failure1);
				resolution.Should().Be(Resolution.Stopped);
			}
		}

		[Test]
		[Description("Verifies that the death of the host process is detected when its caused by an access violation")]
		public void TestFailureDetection4()
		{
			using (var handle = new ManualResetEvent(false))
			{
				Resolution? resolution = null;

				var handler = new Mock<IFailureHandler>();
				handler.Setup(x => x.OnResolutionFinished(It.IsAny<Failure>(), It.IsAny<Decision>(), It.IsAny<Resolution>()))
				       .Callback((Failure f, Decision d, Resolution r) =>
					       {
						       resolution = r;
						       handle.Set();
					       });

				using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: handler.Object))
				{
					silo.Start();
					IVoidMethodNoParameters proxy = silo.CreateGrain<IVoidMethodNoParameters, CausesAccessViolation>();

					new Action(proxy.Do).Should().Throw<ConnectionLostException>();

					handle.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
					resolution.Should().Be(Resolution.Stopped);
				}
			}
		}
	}
}