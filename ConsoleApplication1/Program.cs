using System;
using System.Diagnostics;
using System.Threading;
using SharpRemote.Hosting;
using SharpRemote.Test.Hosting;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace ConsoleApplication1
{
	internal class Program
	{
		private static void Main(string[] args)
		{
			OneClientSync();
			//ManyClientsAsync();

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

		private static void ManyClientsAsync()
		{
			var time = TimeSpan.FromSeconds(5);
			int num = 0;

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				var grain = silo.CreateGrain<IGetInt64Property, ReturnsNearlyInt64Max>();
				// Optimization phase
				const int numOptPasses = 100;
				long sum = 0;
				for (int i = 0; i < numOptPasses; ++i)
				{
					sum += grain.Value;
				}

				// Measurement phase
				const int numClients = 16;
				var clients = new Thread[numClients];
				for (int clientIndex = 0; clientIndex < numClients; ++clientIndex)
				{
					clients[clientIndex] = new Thread(() =>
					{
						var watch = new Stopwatch();
						const int batchSize = 1000;
						watch.Start();
						while (watch.Elapsed < time)
						{
							for (int i = 0; i < batchSize; ++i)
							{
								unchecked
								{
									sum += grain.Value;
								}
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
				Console.WriteLine("Sent: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesSent), OutOfProcessSiloTest.FormatSize(silo.NumBytesSent / numSeconds));
				Console.WriteLine("Received: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesReceived), OutOfProcessSiloTest.FormatSize(silo.NumBytesReceived / numSeconds));
				Console.WriteLine("RTT: {0}ms", (int)silo.RoundtripTime.TotalMilliseconds);
			}
		}

		private static void OneClientSync()
		{
			TimeSpan time = TimeSpan.FromSeconds(10);
			var watch = new Stopwatch();
			int num = 0;

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				IGetInt64Property grain = silo.CreateGrain<IGetInt64Property, ReturnsNearlyInt64Max>();

				long sum = 0;

				// Optimization phase
				for (int i = 0; i < 100; ++i)
				{
					unchecked
					{
						sum += grain.Value;
					}
				}

				// Measurement phase
				watch.Start();
				while (watch.Elapsed < time)
				{
					for (int i = 0; i < 100; ++i)
					{
						unchecked
						{
							sum += grain.Value;
						}
					}
					num += 100;
				}
				watch.Stop();

				double numSeconds = watch.Elapsed.TotalSeconds;
				double ops = 1.0*num/numSeconds;
				Console.WriteLine("Total calls: {0} (sum: {1})", num, sum);
				Console.WriteLine("OP/s: {0:F2}k/s", ops/1000);
				Console.WriteLine("Sent: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesSent),
				                  OutOfProcessSiloTest.FormatSize((long) (silo.NumBytesSent/numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesReceived),
				                  OutOfProcessSiloTest.FormatSize((long) (silo.NumBytesReceived/numSeconds)));
				Console.WriteLine("RTT: {0}ms", (int) silo.RoundtripTime.TotalMilliseconds);
			}
		}
	}
}