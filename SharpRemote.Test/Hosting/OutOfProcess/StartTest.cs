using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Exceptions;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Core;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	public sealed class StartTest
		: AbstractTest
	{
		public override LogItem[] Loggers
		{
			get
			{
				return new[]
					{
						new LogItem(typeof (OutOfProcessSilo), Level.Warn)
					};
			}
		}

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
		[Repeat(10)]
		[LocalTest("Puts too much stress on AppVeyor")]
		[NUnit.Framework.Description("Verifies that multiple silos can be started concurrently")]
		public void TestStart4()
		{
			const int taskCount = 16;
			var failureHandler = new RestartOnFailureStrategy();
			var tasks = new Task[taskCount];

			for (int i = 0; i < taskCount; ++i)
			{
				tasks[i] = new Task(() =>
					{
						using (var silo = new OutOfProcessSilo(failureHandler: failureHandler))
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

		[Test]
		[NUnit.Framework.Description("Verifies that Start() gives up restarting the (defunct) application after a limited amount of times")]
		public void TestStart7()
		{
			using (var silo = new OutOfProcessSilo("SharpRemote.Host.FailsStartup.exe",
				failureHandler: new RestartOnFailureStrategy(startFailureThreshold: 20)))
			{
				new Action(silo.Start)
					.ShouldThrow<AggregateException>();

				silo.IsProcessRunning.Should().BeFalse();
			}
		}

		class MyFailureHandler: IFailureHandler
		{
			private string _directory;

			public MyFailureHandler(string dir)
			{
				_directory = dir;
			}

			public event Action<int, Exception> OnStartFailureCalled;

			public Decision? OnStartFailure(int numSuccessiveFailures, Exception hostProcessException, out TimeSpan waitTime)
			{
				var fn = OnStartFailureCalled;
				if (fn != null)
					fn(numSuccessiveFailures, hostProcessException);

				waitTime = TimeSpan.Zero;

				if (numSuccessiveFailures == 1)
				{
					Copy("SharpRemote.dll", _directory);
					return Decision.RestartHost;
				}

				return Decision.Stop;
			}

			public Decision? OnFailure(Failure failure)
			{
				return null;
			}

			public void OnResolutionFailed(Failure failure, Decision decision, Exception exception)
			{
				
			}

			public void OnResolutionFinished(Failure failure, Decision decision, Resolution resolution)
			{
				
			}
		}

		[Test]
		[LocalTest("Doesn't work ")]
		[NUnit.Framework.Description("Verifies that Start() queries the failure handler when the process fails to be started AND actually tries again")]
		public void TestStart8()
		{
			var dir = Path.Combine(Path.GetTempPath(), "SharpRemote", Guid.NewGuid().ToString());

			var failureHandler = new MyFailureHandler(dir);
			var onStartFailureCalled = new List<KeyValuePair<int, Exception>>();
			failureHandler.OnStartFailureCalled += (numFailures, exception) => onStartFailureCalled.Add(new KeyValuePair<int, Exception>(numFailures, exception));

			// Let's start by copying the host executable to a new folder, but let's conveniently forget
			// an import assembly. This way Start will definately fail...
			var executable = Copy("SharpRemote.Host.exe", dir);
			Copy("log4net.dll", dir);

			using (var silo = new OutOfProcessSilo(executable,
				failureHandler: failureHandler))
			{
				new Action(silo.Start)
					.ShouldNotThrow("Because the error will be corrected after the first start fails");

				onStartFailureCalled.Count.Should().Be(1, "Because starting the application should've failed only once");
				onStartFailureCalled[0].Key.Should().Be(1);
				onStartFailureCalled[0].Value.Should().BeOfType<HandshakeException>();
				silo.IsProcessRunning.Should().BeTrue();
			}
		}

		private static string Copy(string fileName, string dir)
		{
			bool exists = Directory.Exists(dir);
			if (!exists)
				Directory.CreateDirectory(dir);

			var destFileName = Path.Combine(dir, fileName);
			File.Copy(fileName, destFileName);
			return destFileName;
		}
	}
}