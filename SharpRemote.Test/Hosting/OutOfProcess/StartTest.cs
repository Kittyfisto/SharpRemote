using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Exceptions;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	public sealed class StartTest
		: AbstractTest
	{
		[Test]
		[NUnit.Framework.Description("Verifies that starting the default host process succeeds")]
		public void TestStart1()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.IsProcessRunning.Should().BeFalse();
				silo.Start();

				silo.IsProcessRunning.Should().BeTrue();
				silo.HasProcessFailed.Should().BeFalse();
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that starting a non-existant executable throws")]
		public void TestStart2()
		{
			using (var silo = new OutOfProcessSilo("Doesntexist.exe"))
			{
				silo.IsProcessRunning.Should().BeFalse();

				new Action(silo.Start)
					.ShouldThrow<FileNotFoundException>()
					.WithMessage("The system cannot find the file specified");

				silo.IsProcessRunning.Should().BeFalse();
				silo.HasProcessFailed.Should().BeFalse();

				new Action(() => silo.CreateGrain<IVoidMethodInt32Parameter>())
					.ShouldThrow<NotConnectedException>();
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that starting a non executable throws")]
		public void TestStart3()
		{
			using (var silo = new OutOfProcessSilo("SharpRemote.dll"))
			{
				silo.IsProcessRunning.Should().BeFalse();

				new Action(silo.Start)
					.ShouldThrow<Win32Exception>()
					.WithMessage("The specified executable is not a valid application for this OS platform.");

				silo.IsProcessRunning.Should().BeFalse();
				silo.HasProcessFailed.Should().BeFalse();

				new Action(() => silo.CreateGrain<IVoidMethodInt32Parameter>())
					.ShouldThrow<NotConnectedException>();
			}
		}

		[Test]
		[LocalTest("Puts too much stress on AppVeyor")]
		[NUnit.Framework.Description("Verifies that multiple silos can be started concurrently")]
		public void TestStart4()
		{
			const int taskCount = 16;
			var tasks = new Task[taskCount];
			for (int i = 0; i < taskCount; ++i)
			{
				tasks[i] = new Task(() =>
					{
						using (var silo = new OutOfProcessSilo())
						{
							silo.IsProcessRunning.Should().BeFalse();
							silo.Start();
							silo.IsProcessRunning.Should().BeTrue();

							var proxy = silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
							proxy.Value.Should().Be("Foobar");
						}
					});
				tasks[i].Start();
			}
			Task.WaitAll(tasks);
		}

		[Test]
		[NUnit.Framework.Description("Verifies that calling Start() after it has succeeded is not allowed")]
		public void TestStart5()
		{
			using (var silo = new OutOfProcessSilo())
			{
				new Action(silo.Start).ShouldNotThrow();
				new Action(silo.Start).ShouldThrow<InvalidOperationException>();
			}
		}

		[Test]
		[NUnit.Framework.Description("Verifies that exceptions thrown in the child process are marshalled back")]
		public void TestStart6()
		{
			using (var silo = new OutOfProcessSilo("SharpRemote.Host.FailsStartup.exe"))
			{
				new Action(silo.Start)
					.ShouldThrow<HandshakeException>()
					.WithMessage(
						"Process 'SharpRemote.Host.FailsStartup.exe' caught an unexpected exception during startup and subsequently failed")
					.WithInnerException<FileNotFoundException>()
					.WithInnerMessage("Shit happens");
			}
		}
	}
}