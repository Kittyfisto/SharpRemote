using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Diagnostics;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class HeartbeatMonitorTest
		: AbstractTest
	{
		[SetUp]
		public void SetUp2()
		{
			_heartbeat = new Mock<IHeartbeat>();
			_debugger = new Mock<IDebugger>();
			_connectionId = new ConnectionId(2);
		}

		private Mock<IHeartbeat> _heartbeat;
		private Mock<IDebugger> _debugger;
		private ConnectionId _connectionId;

		private void TestFailure(IDebugger debugger, bool enabledWithAttachedDebugger)
		{
			long actualNumHeartbeats = 0;
			DateTime? failureStarted = null;
			_heartbeat.Setup(x => x.Beat())
			          .Returns(() => Task.Factory.StartNew(() =>
				          {
					          // Let's simulate failure by blocking the task
					          if (++actualNumHeartbeats == 25)
					          {
						          failureStarted = DateTime.Now;
						          Thread.Sleep(10000);
					          }
				          }));

			HeartbeatMonitor monitor;
			using (
				monitor =
				new HeartbeatMonitor(_heartbeat.Object,
					debugger,
					TimeSpan.FromSeconds(0.01),
					1,
					enabledWithAttachedDebugger, true, _connectionId))
			{
				bool failureDetected = false;
				ConnectionId actualId = ConnectionId.None;
				monitor.OnFailure += id =>
					{
						failureDetected = true;
						actualId = id;
					};
				monitor.Start();

				Thread.Sleep(TimeSpan.FromSeconds(1));
				Console.WriteLine("# heartbeats: {0}", monitor.NumHeartbeats);

				monitor.FailureDetected.Should().BeTrue();
				failureDetected.Should().BeTrue();
				actualId.Should().Be(_connectionId);
				monitor.NumHeartbeats.Should().Be(24, "Because failure was initiated on the 25th heartbeat");
				monitor.LastHeartbeat.Should().BeOnOrBefore(failureStarted.Value);
			}
		}

		[Test]
		public void TestCtor1()
		{
			var monitor = new HeartbeatMonitor(_heartbeat.Object,
			                                   Debugger.Instance,
			                                   TimeSpan.FromSeconds(2),
			                                   4,
			                                   true,
			                                   true,
			                                   _connectionId);
			monitor.Interval.Should().Be(TimeSpan.FromSeconds(2));
			monitor.FailureInterval.Should()
			       .Be(TimeSpan.FromSeconds(10),
			           "because we specified that 4 skipped heartbeats are to indicate failure which translates to 10 seconds");
			monitor.IsStarted.Should().BeFalse();
			monitor.IsDisposed.Should().BeFalse();
		}

		[Test]
		[Description("Verifies that specifying a null heartbeat interface is not allowed")]
		public void TestCtor2()
		{
			new Action(() => new HeartbeatMonitor(null, Debugger.Instance, TimeSpan.FromSeconds(1), 2, true, true, _connectionId))
				.ShouldThrow<ArgumentException>()
				.WithMessage("Value cannot be null.\r\nParameter name: heartbeat");
		}

		[Test]
		[Description("Verifies that specifying a null debugger interface is not allowed")]
		public void TestCtor3()
		{
			new Action(() => new HeartbeatMonitor(_heartbeat.Object, null, TimeSpan.FromSeconds(1), 2, true, true, _connectionId))
				.ShouldThrow<ArgumentException>()
				.WithMessage("Value cannot be null.\r\nParameter name: debugger");
		}

		[Test]
		[Description("Verifies that specifying a negative heartbeat interval is not allowed")]
		public void TestCtor4()
		{
			new Action(() => new HeartbeatMonitor(_heartbeat.Object, Debugger.Instance, TimeSpan.FromSeconds(-1), 2, true, true, _connectionId))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: heartBeatInterval");
		}

		[Test]
		[Description("Verifies that specifying less than 1 skipped heartbeat as a failure threshold is not allowed")]
		public void TestCtor5()
		{
			new Action(() => new HeartbeatMonitor(_heartbeat.Object, Debugger.Instance, TimeSpan.FromSeconds(2), 0, true, true, _connectionId))
				.ShouldThrow<ArgumentException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: failureThreshold");
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that failures are reported when no debugger is attached")]
		public void TestDetectFailure1()
		{
			const bool enabledWithAttachedDebugger = false;
			_debugger.Setup(x => x.IsDebuggerAttached).Returns(false);

			TestFailure(_debugger.Object, enabledWithAttachedDebugger);
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that failures are reported when no debugger is attached")]
		public void TestDetectFailure2()
		{
			const bool enabledWithAttachedDebugger = true;
			_debugger.Setup(x => x.IsDebuggerAttached).Returns(false);

			TestFailure(_debugger.Object, enabledWithAttachedDebugger);
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that failures are detected when the debugger is attached, but failures should be reported envertheless")]
		public void TestDetectFailure3()
		{
			const bool enabledWithAttachedDebugger = true;
			_debugger.Setup(x => x.IsDebuggerAttached).Returns(true);

			TestFailure(_debugger.Object, enabledWithAttachedDebugger);
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that failures are not detected, when the debugger is attached and the monitor is configured to not report failures with an attached debugger")]
		public void TestDetectFailure4()
		{
			const bool enabledWithAttachedDebugger = false;
			_debugger.Setup(x => x.IsDebuggerAttached).Returns(true);

			long actualNumHeartbeats = 0;
			DateTime? failureStarted = null;
			_heartbeat.Setup(x => x.Beat())
					  .Returns(() => Task.Factory.StartNew(() =>
					  {
						  // Let's simulate failure by blocking the task
						  if (++actualNumHeartbeats == 25)
						  {
							  failureStarted = DateTime.Now;
							  Thread.Sleep(10000);
						  }
					  }));

			HeartbeatMonitor monitor;
			using (
				monitor =
				new HeartbeatMonitor(_heartbeat.Object, _debugger.Object, TimeSpan.FromSeconds(0.01), 1, enabledWithAttachedDebugger, true, _connectionId))
			{
				bool failureDetected = false;
				ConnectionId actualId = ConnectionId.None;
				monitor.OnFailure += id =>
					{
						failureDetected = true;
						actualId = id;
					};
				monitor.Start();

				Thread.Sleep(TimeSpan.FromSeconds(1));
				Console.WriteLine("# heartbeats: {0}", monitor.NumHeartbeats);

				const string reason = "Because the debugger is attached and no failures shall be reported when this is the case";
				monitor.FailureDetected.Should().BeFalse(reason);
				failureDetected.Should().BeFalse(reason);
				actualId.Should().Be(ConnectionId.None);
				failureStarted.Should().HaveValue();
				monitor.NumHeartbeats.Should().BeGreaterOrEqualTo(25);
			}
		}

		[Test]
		[Repeat(50)]
		public void TestTaskExceptionObservation()
		{
			var settings = new HeartbeatSettings
				{
					Interval = TimeSpan.FromMilliseconds(10)
				};

			var exceptions = new List<Exception>();
			TaskScheduler.UnobservedTaskException += (sender, args) => exceptions.Add(args.Exception);

			using (var heartbeatFailure = new ManualResetEvent(false))
			using (
				var monitor = new HeartbeatMonitor(_heartbeat.Object, _debugger.Object, settings, _connectionId))
			{
				_heartbeat.Setup(x => x.Beat())
							.Returns(() =>
							{
								var task = new Task(() =>
								{
									heartbeatFailure.WaitOne();
									throw new ConnectionLostException();
								});
								task.Start();
								return task;
							});

				monitor.OnFailure += id => heartbeatFailure.Set();
				monitor.Start();

				heartbeatFailure.WaitOne(TimeSpan.FromMilliseconds(500))
					.Should().BeTrue("Because the task doesn't return before a failure was reported");
			}

			GC.Collect(2, GCCollectionMode.Forced);
			GC.WaitForPendingFinalizers();

			exceptions.Should().Equal(new Exception[0]);
		}

		[Test]
		[Description("Verifies that Dispose sets the IsStarted property to false, even when Stop() hasn't been called")]
		public void TestDispose()
		{
			HeartbeatMonitor monitor;
			using (monitor = new HeartbeatMonitor(_heartbeat.Object, Debugger.Instance, new HeartbeatSettings(), _connectionId))
			{
				monitor.IsDisposed.Should().BeFalse();

				monitor.Start();
				monitor.IsStarted.Should().BeTrue();
			}
			monitor.IsStarted.Should().BeFalse();
			monitor.IsDisposed.Should().BeTrue();
		}

		[Test]
		[Description("Verifies that Start() sets the IsStarted property to true")]
		public void TestStart()
		{
			using (var monitor = new HeartbeatMonitor(_heartbeat.Object, Debugger.Instance, new HeartbeatSettings(), _connectionId))
			{
				monitor.IsStarted.Should().BeFalse();
				monitor.Start();
				monitor.IsStarted.Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that Stop() sets the IsStarted property to false")]
		public void TestStop()
		{
			using (var monitor = new HeartbeatMonitor(_heartbeat.Object, Debugger.Instance, new HeartbeatSettings(), _connectionId))
			{
				monitor.Start();
				monitor.IsStarted.Should().BeTrue();
				monitor.Stop();
				monitor.IsStarted.Should().BeFalse();
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description(
			"Verifies that the monitor invokes the heartbeat interface once started and correctly counts the amount of invocations"
			)]
		public void TestSuccess()
		{
			long actualNumHeartbeats = 0;
			_heartbeat.Setup(x => x.Beat())
			          .Returns(() => Task.Factory.StartNew(() => { Interlocked.Increment(ref actualNumHeartbeats); }));

			HeartbeatMonitor monitor;
			using (monitor = new HeartbeatMonitor(_heartbeat.Object, Debugger.Instance, TimeSpan.FromSeconds(0.1), 1, true, true, _connectionId))
			{
				bool failureDetected = false;
				monitor.OnFailure += unuse => failureDetected = true;
				monitor.Start();

				Thread.Sleep(TimeSpan.FromSeconds(1));
				Console.WriteLine("# heartbeats: {0}", monitor.NumHeartbeats);

				monitor.FailureDetected.Should().BeFalse();
				failureDetected.Should().BeFalse();
			}

			// There should be 10 heartbeats in a perfect world, let's just verify that we've got half of that
			monitor.NumHeartbeats.Should().BeGreaterOrEqualTo(5);
		}
	}
}