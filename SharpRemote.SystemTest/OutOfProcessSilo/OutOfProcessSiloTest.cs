using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using log4net.Core;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test;
using SharpRemote.Test.CodeGeneration.Serialization;
using SharpRemote.Test.Hosting.OutOfProcess;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.SystemTest.OutOfProcessSilo
{
	[TestFixture]
	public sealed class OutOfProcessSiloTest
	{
		public static string FormatSize(long numBytesSent)
		{
			const long oneKilobyte = 1024;
			const long oneMegabyte = 1024*oneKilobyte;
			const long oneGigabyte = 1024*oneMegabyte;

			if (numBytesSent > oneGigabyte)
				return string.Format("{0:F2} Gb", 1.0*numBytesSent/oneGigabyte);

			if (numBytesSent > oneMegabyte)
				return string.Format("{0:F2} Mb", 1.0*numBytesSent/oneMegabyte);

			if (numBytesSent > oneKilobyte)
				return string.Format("{0:F2} Kb", 1.0*numBytesSent/oneKilobyte);

			return string.Format("{0} bytes", numBytesSent);
		}

		private static bool IsProcessRunning(int pid)
		{
			try
			{
				using (var process = Process.GetProcessById(pid))
				{
					return !process.HasExited;
				}
			}
			catch (Exception)
			{
				return false;
			}
		}

		private void ProcessShouldBeRunning(int pid)
		{
			this.Property(x => IsProcessRunning(pid)).ShouldEventually().BeTrue();
		}

		private void ProcessShouldNotBeRunning(int pid)
		{
			this.Property(x => IsProcessRunning(pid)).ShouldEventually().BeFalse();
		}

		[Test]
		[Description("Verifies that a servant can be created on a not-started silo")]
		public void TestCreateServant()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				var servant = silo.CreateServant(42, new Mock<IVoidMethod>().Object);
				servant.Should().NotBeNull();
			}
		}

		[Test]
		[Description("Verifies that calling Stop() on a not-started silo is allowed and is effectively a NOP")]
		public void TestStop()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.IsProcessRunning.Should().BeFalse("because Start() hasn't been called yet");
				silo.HostProcessId.Should().NotHaveValue("because Start() hasn't been called yet");
				silo.IsConnected.Should().BeFalse("because Start() hasn't been called yet");

				silo.Stop();
				silo.IsProcessRunning.Should().BeFalse("because Start() hasn't been called yet");
				silo.HostProcessId.Should().NotHaveValue("because Start() hasn't been called yet");
				silo.IsConnected.Should().BeFalse("because Start() hasn't been called yet");
			}
		}

		[Test]
		[Description("Verifies that Start() starts the host process and Stop() ends it")]
		public void TestStartStop1()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();
				silo.IsProcessRunning.Should().BeTrue("because Start() has been called and the host should be running now");
				silo.IsConnected.Should().BeTrue("because Start() has been called and the connection to the host should be running");
				var pid = silo.HostProcessId;
				pid.Should().HaveValue();
				ProcessShouldBeRunning(pid.Value);

				silo.Stop();
				silo.IsProcessRunning.Should().BeFalse();
				silo.HostProcessId.Should().NotHaveValue();

				ProcessShouldNotBeRunning(pid.Value);
			}
		}

		[Test]
		[Description("Verifies that if Stop() is called, then the process is not immediately restarted")]
		public void TestStartStop2()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();
				silo.Stop();

				Thread.Sleep(TimeSpan.FromSeconds(1));
				silo.IsProcessRunning.Should().BeFalse();
				silo.HostProcessId.Should().NotHaveValue();
			}
		}

		[Test]
		[Description("Verifies that if Stop() is called, then the custom failure handler is NOT invoked")]
		public void TestStartStop3()
		{
			var failureHandler = new FailureHandlerMock();
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: failureHandler))
			{
				silo.Start();
				silo.Stop();

				Thread.Sleep(TimeSpan.FromSeconds(1));

				const string reason =
					"because no failure should've occured (due to the intentional shutdown) and therefore the callback may not have been invoked";
				failureHandler.NumStartFailure.Should().Be(0, reason);
				failureHandler.NumFailure.Should().Be(0, reason);
				failureHandler.NumResolutionFailed.Should().Be(0, reason);
				failureHandler.NumResolutionFinished.Should().Be(0, reason);
			}
		}

		[Test]
		[Description("Verifies that if Stop() is called, then NO warning/error is logged about the process having been killed")]
		public void TestStartStop4()
		{
			using (var collector = new LogCollector("SharpRemote", Level.Warn, Level.Error, Level.Fatal))
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();
				silo.Stop();

				Thread.Sleep(TimeSpan.FromSeconds(1));

				collector.Events.Should().NotContain(x => x.RenderedMessage.Contains("exited unexpectedly"));
				collector.Events.Should().NotContain(x => x.Level >= Level.Warn);
			}
		}

		[Test]
		[Description("Verifies that a stopped silo can be started again")]
		public void TestStartStopStart1()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();
				silo.Stop();

				silo.Start();
				silo.IsProcessRunning.Should().BeTrue();
				var pid = silo.HostProcessId;
				pid.Should().HaveValue();
				ProcessShouldBeRunning(pid.Value);
			}
		}

		[Test]
		[LocalTest("Test doesn't work reliably")]
		[Description("Verifies that new grains can be created once the host process is restarted again")]
		public void TestStartStopStart2()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();

				var proxy = silo.CreateGrain<IReturnsType>(typeof(ReturnsTypeofString));
				proxy.Do().Should().Be<string>();

				silo.Stop();

				new Action(() => proxy.Do())
					.ShouldThrow<RemoteProcedureCallCanceledException>();
				new Action(() => silo.CreateGrain<IReturnsType>(typeof(ReturnsTypeofString)))
					.ShouldThrow<RemoteProcedureCallCanceledException>();

				silo.Start();

				var newProxy = silo.CreateGrain<IReturnsType>(typeof(ReturnsTypeofString));
				newProxy.Do().Should().Be<string>();
			}
		}

		[Test]
		[Description("Verifies that if the hosting process is killed by external forces, then a warning/error is logged stating that")]
		public void TestKillHostingProcess()
		{
			using (var logCollector = new LogCollector("SharpRemote", Level.Info, Level.Warn, Level.Error, Level.Fatal))
			{
				int? pid;
				using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
				{
					silo.Start();

					pid = silo.HostProcessId;
					var process = Process.GetProcessById(pid.Value);
					process.Kill();

					Thread.Sleep(TimeSpan.FromMilliseconds(100));
				}

				var expectedMessage = string.Format("Host '{0}' (PID: {1}) exited unexpectedly with error code -1",
					ProcessWatchdog.SharpRemoteHost,
					pid);
				var @event = logCollector.Events.FirstOrDefault(x => x.RenderedMessage.Contains(expectedMessage));
				@event.Should().NotBeNull("because a message should've been logged that the process exited unexpectedly");
				@event.Level.Should().Be(Level.Error);

				logCollector.Events.Should()
					.NotContain(x => x.RenderedMessage.Contains("Caught exception while disposing "));
			}
		}

		[Test]
		[Description("Verifies that the create method uses the custom type resolver, if specified, to resolve types")]
		public void TestCreate()
		{
			var customTypeResolver = new CustomTypeResolver1();
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(codeGenerator: new CodeGenerator(customTypeResolver)))
			{
				silo.Start();

				customTypeResolver.GetTypeCalled.Should().Be(0);
				var grain = silo.CreateGrain<IReturnsType>(typeof (ReturnsTypeofString));
				customTypeResolver.GetTypeCalled.Should()
				                  .Be(0, "because the custom type resolver in this process didn't need to resolve anything yet");

				grain.Do().Should().Be<string>();
				customTypeResolver.GetTypeCalled.Should()
				                  .Be(1,
				                      "Because the custom type resolver in this process should've been used to resolve typeof(string)");
			}
		}

		[Test]
		public void TestCreateGrain1()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();

				var proxy = silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
				proxy.Value.Should().Be("Foobar");
			}
		}

		[Test]
		public void TestDispose1()
		{
			SharpRemote.Hosting.OutOfProcessSilo silo;
			using (silo = new SharpRemote.Hosting.OutOfProcessSilo())
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
			bool failureDetected = false;
			var settings = new FailureSettings
				{
					HeartbeatSettings =
						{
							Interval = TimeSpan.FromMilliseconds(10)
						}
				};

			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.OnFailure(It.IsAny<Failure>()))
			       .Callback((Failure unused) => failureDetected = true);

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureSettings: settings, failureHandler: handler.Object))
			{
				silo.Start();
				failureDetected.Should().BeFalse("Because the host process shouldn't have failed now");
			}

			Thread.Sleep(100);

			failureDetected.Should()
			             .BeFalse(
				             "Because even though the process is no longer running, the silo shouldn't have reported a failure because it's been properly disposed of");
		}

		[Test]
		public void TestDispose3()
		{
			var silo = new SharpRemote.Hosting.OutOfProcessSilo();
			new Action(silo.Dispose)
				.ShouldNotThrow();
		}

		[Test]
		[Description("Verifies that warning/exception is logged upon disposing when the silo was never started")]
		public void TestDispose4()
		{
			using (var logCollector = new LogCollector("SharpRemote", Level.Info,
				Level.Warn, Level.Error, Level.Fatal))
			{
				var silo = new SharpRemote.Hosting.OutOfProcessSilo();
				new Action(silo.Dispose)
					.ShouldNotThrow();

				logCollector.Log.Should().NotContain("Caught exception while disposing");
				logCollector.Log.Should().NotContain("SharpRemote.NotConnectedException");
			}
		}

		[Test]
		[Description("Verifies that warning/exception is logged upon disposing when the silo was stoppoed beforehand")]
		public void TestStartStopDispose()
		{
			using (var logCollector = new LogCollector("SharpRemote", Level.Info,
				Level.Warn, Level.Error, Level.Fatal))
			{
				var silo = new SharpRemote.Hosting.OutOfProcessSilo();
				silo.Start();
				silo.Stop();

				new Action(silo.Dispose)
					.ShouldNotThrow();

				logCollector.Log.Should().NotContain("Caught exception while disposing");
				logCollector.Log.Should().NotContain("SharpRemote.NotConnectedException");
			}
		}

		[Test]
		public void TestGetProperty()
		{
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();
				IGetInt64Property grain = silo.CreateGrain<IGetInt64Property, ReturnsInt64Max>();
				grain.Value.Should().Be(Int64.MaxValue);
				grain.Value.Should().Be(Int64.MaxValue);
			}
		}

		[Test]
		[LocalTest("There is no point in running these on the CI server")]
		public void TestPerformanceManyClients()
		{
			TimeSpan time = TimeSpan.FromSeconds(5);
			int num = 0;

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();

				IVoidMethod grain = silo.CreateGrain<IVoidMethod, DoesNothing>();
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


				foreach (Thread thread in clients)
				{
					thread.Join();
				}

				int numSeconds = 5;
				double ops = 1.0*num/numSeconds;
				Console.WriteLine("Total calls: {0}", num);
				Console.WriteLine("OP/s: {0:F2}k/s", ops/1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent), FormatSize(silo.NumBytesSent/numSeconds));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived),
				                  FormatSize(silo.NumBytesReceived/numSeconds));
				Console.WriteLine("Latency: {0}ns", (int) silo.RoundtripTime.Ticks*100);
			}
		}

		[Test]
		[LocalTest("There is no point in running these on the CI server")]
		public void TestPerformanceOneClientAsync()
		{
			TimeSpan time = TimeSpan.FromSeconds(5);
			var watch = new Stopwatch();
			int num = 0;

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();

				IReturnsTask grain = silo.CreateGrain<IReturnsTask, ReturnsTask>();

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

				double numSeconds = watch.Elapsed.TotalSeconds;
				double ops = 1.0*num/numSeconds;
				Console.WriteLine("Total calls: {0}", num);
				Console.WriteLine("OP/s: {0:F2}k/s", ops/1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent),
				                  FormatSize((long) (silo.NumBytesSent/numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived),
				                  FormatSize((long) (silo.NumBytesReceived/numSeconds)));
				Console.WriteLine("Latency: {0}ns", (int) silo.RoundtripTime.Ticks*100);
			}
		}

		[Test]
		[LocalTest("")]
		public void TestPerformanceOneClientSync()
		{
			TimeSpan time = TimeSpan.FromSeconds(5);
			var watch = new Stopwatch();
			int num = 0;

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo())
			{
				silo.Start();

				IVoidMethod grain = silo.CreateGrain<IVoidMethod, DoesNothing>();

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

				double numSeconds = watch.Elapsed.TotalSeconds;
				double ops = 1.0*num/numSeconds;
				Console.WriteLine("Total calls: {0}", num);
				Console.WriteLine("OP/s: {0:F2}k/s", ops/1000);
				Console.WriteLine("Sent: {0}, {1}/s", FormatSize(silo.NumBytesSent),
				                  FormatSize((long) (silo.NumBytesSent/numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", FormatSize(silo.NumBytesReceived),
				                  FormatSize((long) (silo.NumBytesReceived/numSeconds)));
				Console.WriteLine("Latency: {0}ns", (int) silo.RoundtripTime.Ticks*100);
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

			SharpRemote.Hosting.OutOfProcessSilo silo;
			using (silo = new SharpRemote.Hosting.OutOfProcessSilo(latencySettings: settings))
			{
				silo.RoundtripTime.Should().Be(TimeSpan.Zero, "because without being started, no latency is measured");
				silo.Start();

				Thread.Sleep(TimeSpan.FromMilliseconds(200));
				TimeSpan rtt = silo.RoundtripTime;
				Console.WriteLine("RTT: {0}Ticks", rtt.Ticks);
				rtt.Should().BeGreaterThan(TimeSpan.Zero);
				rtt.Should().BeLessOrEqualTo(TimeSpan.FromMilliseconds(10));
			}
		}

		[Test]
		[LocalTest("Test doesn't work reliably")]
		[Defect("https://github.com/Kittyfisto/SharpRemote/issues/63")]
		public void TestUseByReferenceTypeAfterRestart()
		{
			using (var logCollector = new LogCollector(new []{ "SharpRemote.EndPoints.ProxyStorage" }, new []{Level.Debug}))
			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: new RestartOnFailureStrategy()))
			{
				logCollector.AutoPrint(TestContext.Progress);
				silo.Start();

				var factory = silo.CreateGrain<IAdvancedFactory>(typeof(AdvancedFactory));
				var proxyToByReferenceClass = factory.Create(typeof(ByReferenceClass));
				var id = GetIdOf(proxyToByReferenceClass);
				Console.WriteLine("ObjectId: {0}", id);

				RestartHost(silo);

				factory = silo.CreateGrain<IAdvancedFactory>(typeof(AdvancedFactory));
				var proxyToObject = factory.Create(typeof(Handle));
				var otherId = GetIdOf(proxyToObject);
				Console.WriteLine("ObjectId: {0}", otherId);
			}
		}

		private void RestartHost(SharpRemote.Hosting.OutOfProcessSilo silo)
		{
			var pid = silo.HostProcessId;
			var process = Process.GetProcessById(pid.Value);
			process.Kill();
			silo.Property(x => x.IsProcessRunning).ShouldEventually().BeFalse();
			silo.Property(x => x.IsProcessRunning).ShouldEventually().BeTrue();
			silo.Property(x => x.IsConnected).ShouldEventually().BeTrue();
		}

		private object GetIdOf(object value)
		{
			var proxy = (IProxy) value;
			return proxy.ObjectId;
		}
	}
}