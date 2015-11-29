using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;
using SharpRemote.Test.Types.Interfaces.NativeTypes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	[LocalTest("")]
	public sealed partial class OutOfProcessSiloTest
		: AbstractTest
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

		[Test]
		[Description("Verifies that the create method uses the custom type resolver, if specified, to resolve types")]
		public void TestCreate()
		{
			var customTypeResolver = new CustomTypeResolver1();
			using (var silo = new OutOfProcessSilo(customTypeResolver: customTypeResolver))
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
			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var proxy = silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
				proxy.Value.Should().Be("Foobar");
			}
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
			var settings = new FailureSettings
				{
					HeartbeatSettings =
						{
							Interval = TimeSpan.FromMilliseconds(10)
						}
				};

			var handler = new Mock<IFailureHandler>();
			handler.Setup(x => x.OnFailure(It.IsAny<Failure>()))
			       .Callback((Failure unused) => faultDetected = true);

			using (var silo = new OutOfProcessSilo(failureSettings: settings, failureHandler: handler.Object))
			{
				silo.Start();
				faultDetected.Should().BeFalse("Because the host process shouldn't fault now");
			}

			Thread.Sleep(100);

			faultDetected.Should()
			             .BeFalse(
				             "Because even though the process is no longer running, the silo shouldn't have reported a fault because it's been properly disposed of");
		}

		[Test]
		public void TestDispose3()
		{
			var silo = new OutOfProcessSilo();
			new Action(silo.Dispose)
				.ShouldNotThrow();
		}

		[Test]
		[Description("Verifies that the silo restarts the host process when it's killed")]
		public void TestFailureRecovery1()
		{
			using (var silo = new OutOfProcessSilo())
			{
			}
		}

		[Test]
		public void TestGetProperty()
		{
			using (var silo = new OutOfProcessSilo())
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

			using (var silo = new OutOfProcessSilo())
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

			using (var silo = new OutOfProcessSilo())
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

			using (var silo = new OutOfProcessSilo())
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

			OutOfProcessSilo silo;
			using (silo = new OutOfProcessSilo(latencySettings: settings))
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
	}
}