using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	public sealed class FailureRecoveryTest
		: AbstractTest
	{
		private OutOfProcessSilo _silo;
		private DefaultFailureHandler _failureHandler;

		public override Type[] Loggers
		{
			get { return new[] {typeof (OutOfProcessSilo)}; }
		}

		[SetUp]
		public new void SetUp()
		{
			_failureHandler = new DefaultFailureHandler();
			_silo = new OutOfProcessSilo(failureHandler: _failureHandler);
		}

		[TearDown]
		public void TearDown()
		{
			_silo.Dispose();
		}

		[Test]
		[Description("Verifies that the host process is restarted if it ended for some reason")]
		public void TestRestart1()
		{
			using (var handle = new ManualResetEvent(false))
			{
				_silo.OnHostStarted += () => handle.Set();
				_silo.Start();

				handle.WaitOne(TimeSpan.FromSeconds(1)).Should().BeTrue();
				handle.Reset();

				var oldPid = _silo.HostProcessId.Value;
				var proc = Process.GetProcessById(oldPid);
				proc.Kill();

				handle.WaitOne(TimeSpan.FromSeconds(1)).Should().BeTrue("because the silo should've restarted the host process automatically");
				var newPid = _silo.HostProcessId;
				newPid.Should().HaveValue();
				newPid.Should().NotBe(oldPid);
			}
		}
	}
}
