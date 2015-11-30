using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	public sealed class OutOfProcessQueueTest
	{
		private ProcessWatchdog _process;
		private SocketRemotingEndPointClient _endPoint;
		private Mock<IFailureHandler> _failureHandler;
		private FailureSettings _failureSettings;
		private OutOfProcessQueue _queue;

		[SetUp]
		public void SetUp()
		{
			_process = new ProcessWatchdog();
			_endPoint = new SocketRemotingEndPointClient();
			_failureHandler = new Mock<IFailureHandler>();
			_failureSettings = new FailureSettings();
			_queue = new OutOfProcessQueue(_process, _endPoint, _failureHandler.Object, _failureSettings);
		}

		[TearDown]
		public void TearDown()
		{
			_queue.Dispose();
			_endPoint.Disconnect();
			_process.Dispose();
		}

		[Test]
		[Description("Verifies that failures that don't reference the current PID are ignored")]
		public void TestIgnoreFailure1()
		{
			_queue.Start().Wait();

			var pid = _queue.CurrentPid;
			pid.Should().NotBe(0);
			var fakePid = pid + 1;

			_queue.DoHandleFailure(OutOfProcessQueue.Operation.HandleFailure(
				Failure.HostProcessExited,
				fakePid
				                       ))
			      .Should().Be(OutOfProcessQueue.OperationResult.Ignored,
			                   "Because the PID of failure is different from the current PID");

			_queue.CurrentPid.Should().Be(pid,
			                              "Because the queue should've ignored the supposed failure and kept old and working process"
				);
		}

		[Test]
		[Description("Verifies that failures that don't reference the current connection are ignored")]
		public void TestIgnoreFailure2()
		{
			_queue.Start().Wait();

			var id = _queue.CurrentConnection;
			id.Should().Be(new ConnectionId(1), "Because the first connection is always 1");
			var fakeId = new ConnectionId(2);

			_queue.DoHandleFailure(OutOfProcessQueue.Operation.HandleFailure(
				Failure.ConnectionClosed,
				fakeId
				                       ))
			      .Should().Be(OutOfProcessQueue.OperationResult.Ignored,
			                   "Because the connection id of failure is different from the current connection id");

			_queue.CurrentConnection.Should().Be(id,
			                                     "Because the queue should've ignored the supposed failure and kept the old and working connection"
				);
		}

		[Test]
		[Description("Verifies that failures that are processed after the queue has been disposed of are ignored")]
		public void TestIgnoreFailure3()
		{
			_queue.Dispose();
			_queue.DoHandleFailure(OutOfProcessQueue.Operation.HandleFailure(
				Failure.HostProcessExited,
				123)).Should().Be(OutOfProcessQueue.OperationResult.Ignored,
				                  "Because the queue has been disposed of and thus any further failures are to be ignored completely");
		}

		[Test]
		[Description("Verifies that failures that reference the current PID are honored and the application is restarted")]
		public void TestHandleFailure1()
		{
			_queue.Start().Wait();

			var pid = _queue.CurrentPid;
			var connectionId = _queue.CurrentConnection;

			_queue.DoHandleFailure(OutOfProcessQueue.Operation.HandleFailure(
				Failure.HostProcessExited,
				pid
				                       ))
			      .Should().Be(OutOfProcessQueue.OperationResult.Processed);

			_queue.CurrentPid.Should().NotBe(pid, "Because the queue should've killed the old process and started a new one");
			_queue.CurrentConnection.Should()
			      .NotBe(connectionId, "Because the queue should've disconnected the old connection and established a new one");
		}

		[Test]
		[Description("Verifies that failures that reference the current connection ID are honored and the application is restarted")]
		public void TestHandleFailure2()
		{
			_queue.Start().Wait();

			var pid = _queue.CurrentPid;
			var connectionId = _queue.CurrentConnection;

			_queue.DoHandleFailure(OutOfProcessQueue.Operation.HandleFailure(
				Failure.HeartbeatFailure,
				connectionId
				                       ))
			      .Should().Be(OutOfProcessQueue.OperationResult.Processed);

			_queue.CurrentPid.Should().NotBe(pid, "Because the queue should've killed the old process and started a new one");
			_queue.CurrentConnection.Should()
				  .NotBe(connectionId, "Because the queue should've disconnected the old connection and established a new one");
		}
	}
}