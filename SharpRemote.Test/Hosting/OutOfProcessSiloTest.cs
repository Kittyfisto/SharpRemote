using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Exceptions;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Core;
using Description = NUnit.Framework.DescriptionAttribute;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	[LocalTest("")]
	public sealed class OutOfProcessSiloTest
		: AbstractTest
	{
		[TestFixtureSetUp]
		public override void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			TestLogger.SetLevel<OutOfProcessSilo>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointServer>(Level.Info);
			TestLogger.SetLevel<SocketRemotingEndPointClient>(Level.Info);
			TestLogger.SetLevel<AbstractIPSocketRemotingEndPoint>(Level.Info);
			TestLogger.SetLevel<AbstractSocketRemotingEndPoint>(Level.Info);
		}

		[Test]
		[Description("Verifies that starting the default host process succeeds")]
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
		[Description("Verifies that starting a non-existant executable throws")]
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
		[Description("Verifies that starting a non executable throws")]
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
		[Description("Verifies that multiple silos can be started concurrently")]
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

							var proxy = silo.CreateGrain<IGetStringProperty>(typeof(GetStringPropertyImplementation));
							proxy.Value.Should().Be("Foobar");
						}
					});
				tasks[i].Start();
			}
			Task.WaitAll(tasks);
		}

		[Test]
		[Description("Verifies that calling Start() after it has succeeded is not allowed")]
		public void TestStart5()
		{
			using (var silo = new OutOfProcessSilo())
			{
				new Action(silo.Start).ShouldNotThrow();
				new Action(silo.Start).ShouldThrow<InvalidOperationException>();
			}
		}

		[Test]
		[Description("Verifies that exceptions thrown in the child process are marshalled back")]
		public void TestStart6()
		{
			using (var silo = new OutOfProcessSilo("SharpRemote.Host.FailsStartup.exe"))
			{
				new Action(silo.Start)
					.ShouldThrow<HandshakeException>()
					.WithMessage("Process 'SharpRemote.Host.FailsStartup.exe' caught an unexpected exception during startup and subsequently failed")
					.WithInnerException<FileNotFoundException>()
					.WithInnerMessage("Shit happens");
			}
		}

		[Test]
		[LocalTest("Time critical tests dont run on the C/I server")]
		[Description("Verifies that latency measurements are performed and that they are sound")]
		public void TestRoundtripTime()
		{
			var settings = new LatencySettings
				{
					Interval = TimeSpan.FromMilliseconds(1),
					NumSamples = 100
				};

			OutOfProcessSilo silo;
			using (silo = new OutOfProcessSilo(latencySettings: settings))
			{
				silo.RoundtripTime.Should().Be(TimeSpan.Zero, "because without being started, no latency is measured");
				silo.Start();

				Thread.Sleep(TimeSpan.FromMilliseconds(200));
				var rtt = silo.RoundtripTime;
				Console.WriteLine("RTT: {0}Ticks", rtt.Ticks);
				rtt.Should().BeGreaterThan(TimeSpan.Zero);
				rtt.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(10));
			}
		}

		[Test]
		public void TestCreateGrain1()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var proxy = silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
				proxy.Value.Should().Be("Foobar");
			}
		}

		[Test]
		public void TestCtor1()
		{
			using (var silo = new OutOfProcessSilo())
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
			new Action(() => new OutOfProcessSilo(null))
				.ShouldThrow<ArgumentNullException>();
		}

		[Test]
		[Description("Verifies that specifying an empty executable name is not allowed")]
		public void TestCtor3()
		{
			new Action(() => new OutOfProcessSilo(""))
				.ShouldThrow<ArgumentException>();
		}

		[Test]
		[Description("Verifies that specifying a whitespace executable name is not allowed")]
		public void TestCtor4()
		{
			new Action(() => new OutOfProcessSilo("	"))
				.ShouldThrow<ArgumentException>();
		}

		[Test]
		[Description("Verifies that the serializer specified in the ctor is actually used - instead of a new one")]
		public void TestCtor5()
		{
			var serializer = new Serializer();
			serializer.IsTypeRegistered<Tree>().Should().BeFalse();

			using (var silo = new OutOfProcessSilo(serializer: serializer))
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
		[Description("Verifies that specifying negative / zero failure timeouts is not allowed")]
		public void TestCtor6()
		{
			new Action(
				() => new OutOfProcessSilo(failureSettings: new FailureSettings {EndPointConnectTimeout = TimeSpan.FromSeconds(-1)}))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: failureSettings.EndPointConnectTimeout");
			new Action(
				() => new OutOfProcessSilo(failureSettings: new FailureSettings { EndPointConnectTimeout = TimeSpan.Zero }))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: failureSettings.EndPointConnectTimeout");
			new Action(
				() => new OutOfProcessSilo(failureSettings: new FailureSettings { ProcessReadyTimeout = TimeSpan.FromSeconds(-1) }))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: failureSettings.ProcessReadyTimeout");
			new Action(
				() => new OutOfProcessSilo(failureSettings: new FailureSettings { ProcessReadyTimeout = TimeSpan.Zero }))
				.ShouldThrow<ArgumentOutOfRangeException>()
				.WithMessage("Specified argument was out of the range of valid values.\r\nParameter name: failureSettings.ProcessReadyTimeout");
		}

		[Test]
		public void TestDispose1()
		{
			OutOfProcessSilo silo;
			using (silo = new OutOfProcessSilo())
			{
				silo.Start();
				silo.IsProcessRunning.Should().BeTrue();
			}

			silo.IsDisposed.Should().BeTrue();
			silo.IsProcessRunning.Should().BeFalse();
		}

		[Test]
		[Repeat(5)]
		[Description("Verifies that the failure event is NEVER raised once a silo has been disposed of")]
		public void TestDispose2()
		{
			bool faultDetected = false;
			var heartbeatSettings = new HeartbeatSettings
				{
					Interval = TimeSpan.FromMilliseconds(10)
				};

			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.DecideFaultResolution(It.IsAny<Failure>()))
			       .Callback((Failure unused) => faultDetected = true);

			using (var silo = new OutOfProcessSilo(heartbeatSettings: heartbeatSettings, failureHandler: handler.Object))
			{
				silo.Start();
				faultDetected.Should().BeFalse("Because the host process shouldn't fault now");
			}

			Thread.Sleep(100);

			faultDetected.Should().BeFalse("Because even though the process is no longer running, the silo shouldn't have reported a fault because it's been properly disposed of");
		}

		[Test]
		public void TestDispose3()
		{
			var silo = new OutOfProcessSilo();
			new Action(silo.Dispose)
				.ShouldNotThrow();
		}

		[Test]
		[Description("Verifies that a crash of the host process is detected when it happens while a method call")]
		public void TestFailureDetection1()
		{
			Failure? failure = null;
			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.DecideFaultResolution(It.IsAny<Failure>()))
				   .Callback((Failure x) => failure = x);

			using (var silo = new OutOfProcessSilo(failureHandler: handler.Object))
			{
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof (KillsProcess));
				new Action(proxy.Do)
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				WaitFor(() => silo.HasProcessFailed, TimeSpan.FromSeconds(1))
					.Should()
					.BeTrue("Because an aborted thread that is currently invoking a remote method call should cause SharpRemote to kill the host process and report failure");
				silo.IsProcessRunning.Should().BeFalse();

				WaitFor(() => failure != null, TimeSpan.FromSeconds(1))
					.Should().BeTrue("Because the IFailureHandler should've been notified in time");

				(failure == Failure.ConnectionFailure ||
				 failure == Failure.HostProcessExited).Should().BeTrue();
			}
		}

		[Test]
		[Description("Verifies that an abortion of the executing thread of a remote method invocation is detected and that it causes a connection loss")]
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

			using (var silo = new OutOfProcessSilo(failureHandler: handler.Object))
			{
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof(AbortsThread));
				new Action(proxy.Do)
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				WaitFor(() => silo.HasProcessFailed, TimeSpan.FromSeconds(1))
					.Should().BeTrue("Because an unexpected exit of the host process counts as a failure");
				silo.IsProcessRunning.Should().BeFalse();

				WaitFor(() => failure != null, TimeSpan.FromSeconds(1)).Should().BeTrue();
				(failure == Failure.ConnectionFailure ||
				 failure == Failure.HostProcessExited).Should().BeTrue();
				resolution.Should().Be(Resolution.Stopped);
			}
		}

		[Test]
		[LocalTest("Timing sensitive tests don't like to run on the CI server")]
		[Description("Verifies that a complete deadlock of the important remoting threads is detected")]
		public void TestFailureDetection3()
		{
			Failure? failure1 = null;
			Failure? failure2 = null;
			Decision? decision = null;
			Resolution? resolution = null;

			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.DecideFaultResolution(It.IsAny<Failure>()))
			       .Callback((Failure f) => failure1 = f);
			handler.Setup(x => x.OnResolutionFinished(It.IsAny<Failure>(), It.IsAny<Decision>(), It.IsAny<Resolution>()))
				   .Callback((Failure f, Decision d, Resolution r) =>
				   {
					   failure2 = f;
					   decision = d;
					   resolution = r;
				   });

			var settings = new HeartbeatSettings
				{
					ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
					Interval = TimeSpan.FromMilliseconds(100),
					SkippedHeartbeatThreshold = 4
				};
			using (var silo = new OutOfProcessSilo(heartbeatSettings: settings, failureHandler: handler.Object))
			{
				silo.Start();

				var proxy = silo.CreateGrain<IVoidMethodNoParameters>(typeof(DeadlocksProcess));
				new Action(() =>
					{
						Task.Factory.StartNew(proxy.Do, TaskCreationOptions.LongRunning)
						    .Wait(TimeSpan.FromSeconds(10))
						    .Should().BeTrue("Because the silo should've detected the deadlock in time");
					})
					.ShouldThrow<ConnectionLostException>("Because the host process is lost while the method is invoked and therefore the connection to the host process was lost and is the reason for the method to not execute properly");

				WaitFor(()=>silo.HasProcessFailed, TimeSpan.FromSeconds(1)).Should().BeTrue("Because the heartbeat mechanism should have detected that the endpoint doesn't respond anymore");
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

				using (var silo = new OutOfProcessSilo(failureHandler: handler.Object))
				{
					silo.Start();
					var proxy = silo.CreateGrain<IVoidMethodNoParameters, CausesAccessViolation>();

					new Action(proxy.Do).ShouldThrow<ConnectionLostException>();

					handle.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
					resolution.Should().Be(Resolution.Stopped);
				}
			}
		}

		[Test]
		[Description("Verifies that unhandled exceptions cause a mini-dump to be created")]
		public void TestFailureDetection5()
		{
			var settings = new PostMortemSettings
			{
				CollectMinidumps = true,
				SuppressErrorWindows = true,
				HandleAccessViolations = true,
				NumMinidumpsRetained = 1,
				MinidumpFolder = Path.Combine(Path.GetTempPath(), "SharpRemote", "dumps"),
				MinidumpName = "Host"
			};

			if (Directory.Exists(settings.MinidumpFolder))
			{
				Directory.Delete(settings.MinidumpFolder, true);
			}

			using (var silo = new OutOfProcessSilo(postMortemSettings: settings))
			{
				silo.Start();
				var proxy = silo.CreateGrain<IVoidMethodNoParameters, CausesAccessViolation>();

				var beforeFailure = DateTime.Now;
				new Action(proxy.Do).ShouldThrow<ConnectionLostException>();
				var afterFailure = DateTime.Now;

				// Not only should a failure have been detected, but a dump should've been created and stored
				// on disk..

				var files = Directory.EnumerateFiles(settings.MinidumpFolder).ToList();
				files.Count.Should().Be(1, "Because exactly one minidump should've been created");

				var file = new FileInfo(files[0]);
				file.Name.Should().EndWith(".dmp");
				file.LastWriteTime.Should().BeOnOrAfter(beforeFailure);
				file.LastWriteTime.Should().BeOnOrBefore(afterFailure);
			}
		}

		[Test]
		[Description("Verifies that an assertion triggered by the host process is intercepted and results in a termination of the process")]
		public void TestFailureDetection6()
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

				var settings = new PostMortemSettings
				{
					SuppressErrorWindows = true,
					HandleCrtAsserts = true,
#if DEBUG
					RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Debug
#else
					RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Release
#endif
				};

				using (var silo = new OutOfProcessSilo(postMortemSettings: settings, failureHandler: handler.Object))
				{
					silo.Start();
					var proxy = silo.CreateGrain<IVoidMethodNoParameters, CausesAssert>();

					var task = Task.Factory.StartNew(() =>
					{
						new Action(proxy.Do).ShouldThrow<ConnectionLostException>();
					});
					task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

					handle.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
					resolution.Should().Be(Resolution.Stopped);
				}
			}
		}

		[Test]
		[Description("Verifies that an assertion triggered by the host process is intercepted and results in a termination of the process")]
		public void TestFailureDetection7()
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

				var settings = new PostMortemSettings
				{
					CollectMinidumps = true,
					SuppressErrorWindows = true,
					HandleCrtAsserts = true,
#if DEBUG
					RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Debug,
#else
					RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Release,
#endif
					NumMinidumpsRetained = 1,
					MinidumpFolder = Path.Combine(Path.GetTempPath(), "SharpRemote", "dumps"),
					MinidumpName = "Host"
				};

				if (Directory.Exists(settings.MinidumpFolder))
				{
					Directory.Delete(settings.MinidumpFolder, true);
				}

				using (var silo = new OutOfProcessSilo(postMortemSettings: settings, failureHandler: handler.Object))
				{
					silo.Start();
					var proxy = silo.CreateGrain<IVoidMethodNoParameters, CausesAssert>();

					var beforeFailure = DateTime.Now;
					new Action(proxy.Do).ShouldThrow<ConnectionLostException>();
					var afterFailure = DateTime.Now;


					// Not only should a failure have been detected, but a dump should've been created and stored
					// on disk..

					var files = Directory.EnumerateFiles(settings.MinidumpFolder).ToList();
					files.Count.Should().Be(1, "Because exactly one minidump should've been created");

					var file = new FileInfo(files[0]);
					file.Name.Should().EndWith(".dmp");
					file.LastWriteTime.Should().BeOnOrAfter(beforeFailure);
					file.LastWriteTime.Should().BeOnOrBefore(afterFailure);
				}
			}
		}

		[Test]
		[Ignore("This shit doesn't work in release mode for some fucking reason")]
		[Description("Verifies that a pure virtual function triggered by the host process is intercepted and results in a termination of the process")]
		public void TestFailureDetection8()
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

				var settings = new PostMortemSettings
					{
						SuppressErrorWindows = true,
						HandleCrtPureVirtualFunctionCalls = true,
#if DEBUG
						RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Debug,
#else
					RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Release,
#endif
					};

				using (var silo = new OutOfProcessSilo(postMortemSettings: settings))
				{
					silo.Start();
					var proxy = silo.CreateGrain<IVoidMethodNoParameters, CausesPureVirtualFunctionCall>();

					var task = Task.Factory.StartNew(() =>
						{
							new Action(proxy.Do).ShouldThrow<ConnectionLostException>();
						});
					//task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
					task.Wait();

					handle.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();
					resolution.Should().Be(Resolution.Stopped);
				}
			}
		}

		[Test]
		[Ignore("This shit doesn't work in release mode for some fucking reason")]
		[Description("Verifies that a pure virtual function call triggered by the host process is intercepted and results in a termination of the process")]
		public void TestFailureDetection9()
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

				var settings = new PostMortemSettings
					{
						CollectMinidumps = true,
						SuppressErrorWindows = true,
						HandleCrtPureVirtualFunctionCalls = true,
#if DEBUG
						RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Debug,
#else
						RuntimeVersions = CRuntimeVersions._110 | CRuntimeVersions.Release,
#endif
						NumMinidumpsRetained = 1,
						MinidumpFolder = Path.Combine(Path.GetTempPath(), "SharpRemote", "dumps"),
						MinidumpName = "Host"
					};

				if (Directory.Exists(settings.MinidumpFolder))
				{
					Directory.Delete(settings.MinidumpFolder, true);
				}

				using (var silo = new OutOfProcessSilo(postMortemSettings: settings))
				{
					silo.Start();
					var proxy = silo.CreateGrain<IVoidMethodNoParameters, CausesPureVirtualFunctionCall>();

					var beforeFailure = DateTime.Now;
					new Action(proxy.Do).ShouldThrow<ConnectionLostException>();
					var afterFailure = DateTime.Now;


					// Not only should a failure have been detected, but a dump should've been created and stored
					// on disk..

					var files = Directory.EnumerateFiles(settings.MinidumpFolder).ToList();
					files.Count.Should().Be(1, "Because exactly one minidump should've been created");

					var file = new FileInfo(files[0]);
					file.Name.Should().EndWith(".dmp");
					file.LastWriteTime.Should().BeOnOrAfter(beforeFailure);
					file.LastWriteTime.Should().BeOnOrBefore(afterFailure);
				}
			}
		}

		[Test]
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
				handler.Setup(x => x.DecideFaultResolution(It.IsAny<Failure>()))
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

				var settings = new HeartbeatSettings
					{
						ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
						Interval = TimeSpan.FromMilliseconds(100),
						SkippedHeartbeatThreshold = 4
					};

				using (var silo = new OutOfProcessSilo(heartbeatSettings: settings, failureHandler: handler.Object))
				{
					silo.Start();
					var id = silo.HostProcessId;
					id.Should().HaveValue();

					var hostProcess = Process.GetProcessById(id.Value);
					hostProcess.Kill();

					WaitHandle.WaitAll(new[] {handle1, handle2}, TimeSpan.FromSeconds(2))
					          .Should().BeTrue("Because the failure should've been detected as well as handled");

					(failure1 == Failure.ConnectionFailure ||
					 failure1 == Failure.HostProcessExited).Should().BeTrue();
					failure2.Should().Be(failure1);
					resolution.Should().Be(Resolution.Stopped);
				}
			}
		}

		[Test]
		[Description("Verifies that the silo can be disposed of from within the FaultHandled event")]
		public void TestFailureDetection11()
		{
			var handler = new Mock<IFailureHandler>();

			var settings = new HeartbeatSettings
			{
				ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = true,
				Interval = TimeSpan.FromMilliseconds(100),
				SkippedHeartbeatThreshold = 4
			};

			using (var silo = new OutOfProcessSilo(heartbeatSettings: settings, failureHandler: handler.Object))
			using (var handle = new ManualResetEvent(false))
			{
				handler.Setup(x => x.OnResolutionFinished(It.IsAny<Failure>(), It.IsAny<Decision>(), It.IsAny<Resolution>()))
					   .Callback((Failure f, Decision d, Resolution r) =>
					   {
						   silo.Dispose();
						   handle.Set();
					   });

				silo.Start();
				var id = silo.HostProcessId;
				id.Should().HaveValue();

				var hostProcess = Process.GetProcessById(id.Value);
				hostProcess.Kill();

				handle.WaitOne(TimeSpan.FromSeconds(2))
					  .Should().BeTrue("Because the failure should've been detected as well as handled");

				silo.IsDisposed.Should().BeTrue();
				silo.HasProcessFailed.Should().BeTrue();
				silo.IsProcessRunning.Should().BeFalse();
			}
		}

		[Test]
		public void TestGetProperty()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();
				var grain = silo.CreateGrain<IGetInt64Property, ReturnsInt64Max>();
				grain.Value.Should().Be(Int64.MaxValue);
				grain.Value.Should().Be(Int64.MaxValue);
			}
		}

		[Test]
		[Description("Verifies that the create method uses the custom type resolver, if specified, to resolve types")]
		public void TestCreate()
		{
			var customTypeResolver = new CustomTypeResolver1();
			using (var silo = new OutOfProcessSilo(customTypeResolver: customTypeResolver))
			{
				silo.Start();

				customTypeResolver.GetTypeCalled.Should().Be(0);
				var grain = silo.CreateGrain<IReturnsType>(typeof(ReturnsTypeofString));
				customTypeResolver.GetTypeCalled.Should().Be(0, "because the custom type resolver in this process didn't need to resolve anything yet");

				grain.Do().Should().Be<string>();
				customTypeResolver.GetTypeCalled.Should().Be(1, "Because the custom type resolver in this process should've been used to resolve typeof(string)");
			}
		}

		[Test]
		[LocalTest("")]
		public void TestPerformanceOneClientSync()
		{
			var time = TimeSpan.FromSeconds(5);
			var watch = new Stopwatch();
			int num = 0;

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var grain = silo.CreateGrain<IVoidMethod, DoesNothing>();

				// Optimization phase
				for (int i = 0; i < 100; ++i)
				{
					grain.DoStuff();
				}

				// Measurement phase
				watch.Start();
				while (watch.Elapsed < time)
				{
					for (int i = 0; i < 100; ++i)
					{
						grain.DoStuff();
					}
					num += 100;
				}
				watch.Stop();

				var numSeconds = watch.Elapsed.TotalSeconds;
				var ops = 1.0 * num / numSeconds;
				Console.WriteLine("Total calls: {0}", num);
				Console.WriteLine("OP/s: {0:F2}k/s", ops/1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent), FormatSize((long) (silo.NumBytesSent/numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived), FormatSize((long)(silo.NumBytesReceived/numSeconds)));
				Console.WriteLine("Latency: {0}ns", (int)silo.RoundtripTime.Ticks * 100);
			}
		}

		[Test]
		[LocalTest("There is no point in running these on the CI server")]
		public void TestPerformanceOneClientAsync()
		{
			var time = TimeSpan.FromSeconds(5);
			var watch = new Stopwatch();
			int num = 0;

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var grain = silo.CreateGrain<IReturnsTask, ReturnsTask>();

				// Optimization phase
				const int numOptPasses = 100;
				var opts = new Task[numOptPasses];
				for (int i = 0; i < numOptPasses; ++i)
				{
					opts[i] = grain.DoStuff();
				}

				Task.WaitAll(opts);

				// Measurement phase

				const int batchSize = 1000;
				var tasks = new Task[batchSize];

				watch.Start();
				while (watch.Elapsed < time)
				{
					for (int i = 0; i < batchSize; ++i)
					{
						tasks[i] = grain.DoStuff();
					}
					Task.WaitAll(tasks);

					num += batchSize;
				}
				watch.Stop();

				var numSeconds = watch.Elapsed.TotalSeconds;
				var ops = 1.0 * num / numSeconds;
				Console.WriteLine("Total calls: {0}", num);
				Console.WriteLine("OP/s: {0:F2}k/s", ops / 1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent), FormatSize((long)(silo.NumBytesSent / numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived), FormatSize((long)(silo.NumBytesReceived / numSeconds)));
				Console.WriteLine("Latency: {0}ns", (int)silo.RoundtripTime.Ticks * 100);
			}
		}

		[Test]
		[LocalTest("There is no point in running these on the CI server")]
		public void TestPerformanceManyClients()
		{
			var time = TimeSpan.FromSeconds(5);
			int num = 0;

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var grain = silo.CreateGrain<IVoidMethod, DoesNothing>();
				// Optimization phase
				const int numOptPasses = 100;

				for (int i = 0; i < numOptPasses; ++i)
				{
					grain.DoStuff();
				}

				// Measurement phase
				const int numClients = 16;
				var clients = new Thread[numClients];
				for (int clientIndex = 0; clientIndex < numClients; ++clientIndex)
				{
					clients[clientIndex] = new Thread(() =>
						{
							var watch = new Stopwatch();
							const int batchSize = 64;
							watch.Start();
							while (watch.Elapsed < time)
							{
								for (int i = 0; i < batchSize; ++i)
								{
									grain.DoStuff();
								}
								num += batchSize;
							}
							watch.Stop();
						});
					clients[clientIndex].Start();
				}


				foreach (var thread in clients)
				{
					thread.Join();
				}

				var numSeconds = 5;
				var ops = 1.0 * num / numSeconds;
				Console.WriteLine("Total calls: {0}", num);
				Console.WriteLine("OP/s: {0:F2}k/s", ops / 1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent), FormatSize(silo.NumBytesSent / numSeconds));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived), FormatSize(silo.NumBytesReceived / numSeconds));
				Console.WriteLine("Latency: {0}ns", (int)silo.RoundtripTime.Ticks*100);
			}
		}

		public static string FormatSize(long numBytesSent)
		{
			const long oneKilobyte = 1024;
			const long oneMegabyte = 1024*oneKilobyte;
			const long oneGigabyte = 1024*oneMegabyte;

			if (numBytesSent > oneGigabyte)
				return string.Format("{0:F2} Gb", 1.0*numBytesSent/oneGigabyte);

			if (numBytesSent > oneMegabyte)
				return string.Format("{0:F2} Mb", 1.0 * numBytesSent / oneMegabyte);

			if (numBytesSent > oneKilobyte)
				return string.Format("{0:F2} Kb", 1.0 * numBytesSent / oneKilobyte);

			return string.Format("{0} bytes", numBytesSent);
		}
	}
}