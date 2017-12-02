using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test;

namespace SharpRemote.SystemTest.OutOfProcessSilo
{
	[TestFixture]
	[LocalTest("Long running system tests are not executed on AppVeyor")]
	public sealed class LongTest
		: AbstractTest
	{
		[DataContract]
		public sealed class Input
		{
			[DataMember]
			public string ImportantName;
			[DataMember]
			public int ImportantNumber;
		}

		[DataContract]
		public sealed class Result
		{
			[DataMember] public long NumInputs;
			[DataMember] public long NumCharacters;
			[DataMember] public long Sum;
		}

		public interface IDoImportStuff
		{
			[Invoke(Dispatch.SerializePerObject)]
			void Work(Input input);

			[Invoke(Dispatch.SerializePerObject)]
			Task WorkAsync(Input input);

			Result Result { get; }
		}

		public sealed class DoesImportantStuff
			: IDoImportStuff
		{
			private long _numInputs;
			private long _numCharacters;
			private long _sum;

			public void Work(Input input)
			{
				++_numInputs;
				_numCharacters += input.ImportantName.Length;
				_sum += input.ImportantNumber;
			}

			public Task WorkAsync(Input input)
			{
				Work(input);
				return Task.FromResult(42);
			}

			public Result Result
			{
				get
				{
					return new Result
						{
							NumInputs = _numInputs,
							NumCharacters = _numCharacters,
							Sum = _sum
						};
				}
			}
		}

		[Test]
		[Description("Verifies that we can continously perform remote method calls over one minute without the connection interrupting")]
		public void Test1MinuteFullLoadSynchronous()
		{
			var handler = new ZeroFailureToleranceStrategy();
			bool failed = false;
			handler.OnResolutionFailedEvent += () => failed = true;

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: handler))
			{
				silo.Start();
				var proxy = silo.CreateGrain<IDoImportStuff, DoesImportantStuff>();

				long numCalls = 0;
				var start = DateTime.Now;
				var last = start;
				var now = start;
				while ((now = DateTime.Now) - start < TimeSpan.FromMinutes(1))
				{
					proxy.Work(new Input
						{
							ImportantName = "Kittyfisto",
							ImportantNumber = 42
						});

					++numCalls;
					var rtt = silo.RoundtripTime;
					var received = silo.NumBytesReceived;
					var sent = silo.NumBytesSent;

					if (now - last > TimeSpan.FromSeconds(10))
					{
						TestContext.Progress.WriteLine("#{0} calls, {1}μs rtt, {2} received, {3} sent",
						                               numCalls,
						                               rtt.Ticks / 10,
						                               FormatSize(received),
						                               FormatSize(sent)
						                              );

						last = now;
					}

					failed.Should().BeFalse("Because the connection shouldn't have failed");
				}

				var result = proxy.Result;
				result.Should().NotBeNull();
				result.NumInputs.Should().Be(numCalls);
				result.NumCharacters.Should().Be("Kittyfisto".Length*numCalls);
				result.Sum.Should().Be(42*numCalls);
			}
		}

		[Test]
		[Description("Verifies that we can continously perform asynchronous remote method calls over one minute without the connection interrupting")]
		public void Test1MinuteFullLoadAsynchronous()
		{
			var handler = new ZeroFailureToleranceStrategy();
			bool failed = false;
			handler.OnResolutionFailedEvent += () => failed = true;

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: handler))
			{
				silo.Start();
				var proxy = silo.CreateGrain<IDoImportStuff, DoesImportantStuff>();

				long numCalls = 0;
				var start = DateTime.Now;
				var last = start;
				var now = start;
				while ((now = DateTime.Now) - start < TimeSpan.FromMinutes(1))
				{
					proxy.WorkAsync(new Input
						{
							ImportantName = "Kittyfisto",
							ImportantNumber = 42
						});

					++numCalls;
					var rtt = silo.RoundtripTime;
					var received = silo.NumBytesReceived;
					var sent = silo.NumBytesSent;

					if (now - last > TimeSpan.FromSeconds(10))
					{
						TestContext.Progress.WriteLine("#{0} calls, {1}μs rtt, {2} received, {3} sent",
						                               numCalls,
						                               rtt.Ticks / 10,
						                               FormatSize(received),
						                               FormatSize(sent)
						                              );

						last = now;
					}

					failed.Should().BeFalse("Because the connection shouldn't have failed");
				}
			}
		}

		[Test]
		[Description("Verifies that we can continously perform asynchronous remote method calls over one minute without the connection interrupting")]
		public void Test1MinuteFullLoadMultipleAsynchronous()
		{
			const int numServants = 16;

			var handler = new ZeroFailureToleranceStrategy();
			bool failed = false;
			handler.OnResolutionFailedEvent += () => failed = true;

			using (var silo = new SharpRemote.Hosting.OutOfProcessSilo(failureHandler: handler))
			{
				silo.Start();

				var proxies = Enumerable.Range(0, numServants)
				                        .Select(unused => silo.CreateGrain<IDoImportStuff, DoesImportantStuff>()).ToArray();

				long numCalls = 0;
				var start = DateTime.Now;
				var last = start;
				var now = start;
				while ((now = DateTime.Now) - start < TimeSpan.FromMinutes(1))
				{
					var input = new Input
						{
							ImportantName = "Kittyfisto",
							ImportantNumber = 42
						};

					foreach (var proxy in proxies)
					{
						proxy.WorkAsync(input);
					}

					++numCalls;
					var rtt = silo.RoundtripTime;
					var received = silo.NumBytesReceived;
					var sent = silo.NumBytesSent;

					if (now - last > TimeSpan.FromSeconds(10))
					{
						TestContext.Progress.WriteLine("#{0} calls, {1}μs rtt, {2} received, {3} sent",
						                               numCalls,
						                               rtt.Ticks / 10,
						                               FormatSize(received),
						                               FormatSize(sent)
						                              );

						last = now;
					}

					failed.Should().BeFalse("Because the connection shouldn't have failed");
				}
			}
		}

		public static string FormatSize(long numBytes)
		{
			if (numBytes > 1024*1024)
			{
				return string.Format("{0:F2} Mb", 1.0*numBytes/1024/1024);
			}
			if (numBytes > 1024)
			{
				return string.Format("{0:F2} Kb", 1.0 * numBytes / 1024);
			}

			return string.Format("{0} b", numBytes);
		}
	}
}