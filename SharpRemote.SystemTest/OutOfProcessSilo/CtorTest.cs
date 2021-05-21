using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.NativeTypes;

namespace SharpRemote.SystemTest.OutOfProcessSilo
{
	[TestFixture]
	public sealed class CtorTest
		: AbstractTest
	{
		[Test]
		public void TestCtor1()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.IsDisposed.Should().BeFalse();
				silo.HasProcessFailed.Should().BeFalse();
				silo.IsProcessRunning.Should().BeFalse();
			}
		}

		[Test]
		[Description("Verifies that specifying a null executable name is not allowed")]
		public void TestCtor2()
		{
			new Action(() => new SharpRemote.Hosting.OutOfProcessSilo(null))
				.Should().Throw<ArgumentNullException>();
		}

		[Test]
		[Description("Verifies that specifying an empty executable name is not allowed")]
		public void TestCtor3()
		{
			new Action(() => new SharpRemote.Hosting.OutOfProcessSilo(""))
				.Should().Throw<ArgumentException>();
		}

		[Test]
		[Description("Verifies that specifying a whitespace executable name is not allowed")]
		public void TestCtor4()
		{
			new Action(() => new SharpRemote.Hosting.OutOfProcessSilo("	"))
				.Should().Throw<ArgumentException>();
		}

		[Test]
		[LocalTest("Won't run on AppVeyor 100% of the time")]
		[Description("Verifies that the code generator specified in the ctor is actually used - instead of a new one")]
		public void TestCtor5()
		{
			var serializer = new BinarySerializer();
			serializer.IsTypeRegistered<Tree>().Should().BeFalse();
			var codeGenerator = new CodeGenerator(serializer);

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(codeGenerator: codeGenerator))
			{
				silo.Start();
				var grain = silo.CreateGrain<IReturnsObjectMethod, ReturnsTree>();
				var tree = grain.GetListener();
				tree.Should().NotBeNull();
				tree.Should().BeOfType<Tree>();
				serializer.IsTypeRegistered<Tree>().Should().BeTrue("Because the serializer specified in the ctor should've been used to deserialize the value returned by the grain; in turn registering it with said serializer");
			}
		}

		[Test]
		[Description("Verifies that specifying negative / zero heartbeat timeouts/thresholds is not allowed")]
		public void TestCtor6()
		{
			new Action(() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings { HeartbeatSettings = { Interval = TimeSpan.Zero } }))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("The heartbeat interval must be greater than zero\r\nParameter name: heartbeatSettings.Interval");

			new Action(() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings{HeartbeatSettings = { Interval = TimeSpan.FromSeconds(-1) }}))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("The heartbeat interval must be greater than zero\r\nParameter name: heartbeatSettings.Interval");

			new Action(() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings { HeartbeatSettings = { SkippedHeartbeatThreshold = 0} }))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("The skipped heartbeat threshold must be greater than zero\r\nParameter name: heartbeatSettings.SkippedHeartbeatThreshold");

			new Action(() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings { HeartbeatSettings = { SkippedHeartbeatThreshold = -1 } }))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("The skipped heartbeat threshold must be greater than zero\r\nParameter name: heartbeatSettings.SkippedHeartbeatThreshold");
		}

		[Test]
		[Description("Verifies that specifying negative / zero failure timeouts is not allowed")]
		public void TestCtor7()
		{
			new Action(
				() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings { EndPointConnectTimeout = TimeSpan.FromSeconds(-1) }))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("EndPointConnectTimeout should be greater than zero\r\nParameter name: failureSettings");
			new Action(
				() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings { EndPointConnectTimeout = TimeSpan.Zero }))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("EndPointConnectTimeout should be greater than zero\r\nParameter name: failureSettings");
			new Action(
				() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings { ProcessReadyTimeout = TimeSpan.FromSeconds(-1) }))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("ProcessReadyTimeout should be greater than zero\r\nParameter name: failureSettings");
			new Action(
				() => new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: new FailureSettings { ProcessReadyTimeout = TimeSpan.Zero }))
				.Should().Throw<ArgumentOutOfRangeException>()
				.WithMessage("ProcessReadyTimeout should be greater than zero\r\nParameter name: failureSettings");
		}

	}
}
