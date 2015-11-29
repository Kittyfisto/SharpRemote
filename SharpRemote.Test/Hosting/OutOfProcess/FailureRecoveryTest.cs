using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	public sealed class FailureRecoveryTest
		: AbstractTest
	{
		private OutOfProcessSilo _silo;
		private RestartOnFailureStrategy _restartOnFailureHandler;
		private ManualResetEvent _startHandle;

		public override Type[] Loggers
		{
			get { return new[] {typeof (OutOfProcessSilo)}; }
		}

		[SetUp]
		public new void SetUp()
		{
			_restartOnFailureHandler = new RestartOnFailureStrategy();
			_silo = new OutOfProcessSilo(failureHandler: _restartOnFailureHandler);

			_startHandle = new ManualResetEvent(false);
		}

		[TearDown]
		public void TearDown()
		{
			_silo.Dispose();
			_startHandle.Dispose();
		}

		[Test]
		[Description("Verifies that the host process is restarted when it's killed")]
		public void TestRestart1()
		{
			_silo.Start();

			_silo.OnHostStarted += () => _startHandle.Set();
			var oldPid = _silo.HostProcessId.Value;
			var proc = Process.GetProcessById(oldPid);
			proc.Kill();

			_startHandle.WaitOne(TimeSpan.FromSeconds(1)).Should().BeTrue("because the silo should've restarted the host process automatically");
			var newPid = _silo.HostProcessId;
			newPid.Should().HaveValue();
			newPid.Should().NotBe(oldPid);
		}

		[Test]
		[Description("Verifies that after the host process is restarted, it can be used again")]
		public void TestRestart2()
		{
			IGetInt32Property someGrain = null;
			_silo.OnHostStarted += () =>
				{
					someGrain = _silo.CreateGrain<IGetInt32Property, ReturnsPid>();
					_startHandle.Set();
				};
			_silo.Start();

			var pid = _silo.HostProcessId.Value;
			someGrain.Value.Should().Be(pid);

			_startHandle.Reset();
			var proc = Process.GetProcessById(pid);
			proc.Kill();

			_startHandle.WaitOne(TimeSpan.FromSeconds(10)).Should().BeTrue("because the silo should've restarted the host process automatically");
			var newPid = _silo.HostProcessId;
			someGrain.Value.Should().Be(newPid);
		}
	}
}
