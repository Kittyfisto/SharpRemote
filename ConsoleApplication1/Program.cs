using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
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
			Simple();
			//OneClientSync();
			//ManyClientsAsync();

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

		private static void Simple()
		{
			var time = TimeSpan.FromSeconds(5);



			const int messageLength = 90;
			long num = 0;

			var clientTask = new Task(() =>
				{
					using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
					{
						client.Connect(new IPEndPoint(IPAddress.Loopback, 9001));

						var stopwatch = new Stopwatch();
						var buffer = new byte[messageLength];
						stopwatch.Start();
						while (stopwatch.Elapsed < time)
						{
							client.Send(buffer);
							client.Receive(buffer);
							++num;
						}
						stopwatch.Stop();
					}
				}, TaskCreationOptions.LongRunning);

			var serverTask = new Task(() =>
			{
				var server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				server.Bind(new IPEndPoint(IPAddress.Loopback, 9001));
				server.Listen(1);
				var socket = server.Accept();

				var stopwatch = new Stopwatch();
				var buffer = new byte[messageLength];
				stopwatch.Start();

				try
				{
					while (stopwatch.Elapsed < time)
					{
						socket.Receive(buffer);
						socket.Send(buffer);
					}
				}
				catch (SocketException)
				{
					
				}

				stopwatch.Stop();
			}, TaskCreationOptions.LongRunning);

			serverTask.Start();
			clientTask.Start();

			clientTask.Wait();
			serverTask.Wait();

			var numSeconds = 5;
			var ops = 1.0 * num / numSeconds;
			Console.WriteLine("Total calls: {0}", num);
			Console.WriteLine("OP/s: {0:F2}k/s", ops / 1000);
			Console.WriteLine("Latency: {0}ns", time.Ticks * 100 / num);
		}

		private static void ManyClientsAsync()
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
						const int batchSize = 1000;
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
				Console.WriteLine("Sent: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesSent), OutOfProcessSiloTest.FormatSize(silo.NumBytesSent / numSeconds));
				Console.WriteLine("Received: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesReceived), OutOfProcessSiloTest.FormatSize(silo.NumBytesReceived / numSeconds));
				Console.WriteLine("Latency: {0}ns", (int)silo.RoundtripTime.Ticks * 100);
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
				Console.WriteLine("Sent: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesSent),
				                  OutOfProcessSiloTest.FormatSize((long) (silo.NumBytesSent/numSeconds)));
				Console.WriteLine("Received: {0}, {1}/s", OutOfProcessSiloTest.FormatSize(silo.NumBytesReceived),
				                  OutOfProcessSiloTest.FormatSize((long) (silo.NumBytesReceived/numSeconds)));
				Console.WriteLine("Latency: {0}ns", (int)silo.RoundtripTime.Ticks * 100);
			}
		}
	}
}