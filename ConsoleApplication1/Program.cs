using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
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
		private static unsafe void Main(string[] args)
		{
#if DEBUG
			var version = CRuntimeVersions._110 | CRuntimeVersions.Debug;
			const string path = @"E:\Code\SharpRemote\bin\win\x86D";
#else
			var version = CRuntimeVersions._110 | CRuntimeVersions.Release;
			const string path = @"E:\Code\SharpRemote\bin\win\x86";
#endif
			SharpRemote.NativeMethods.SetDllDirectory(path);

			SharpRemote.NativeMethods.Init(
				10,
				@"C:\Users\Simon\AppData\Local\Temp\SharpRemote\Dumps\",
				"ConsoleTest"
				);

			SharpRemote.NativeMethods.InstallPostmortemDebugger(true,
			                                                    true,
			                                                    true,
			                                                    true,
			                                                    version);

			//var test = new CausesAssert();
			//test.Do();
			var test = new CausesPurecall();
			test.Do();

			var b = new byte[10];
			fixed (byte* ptr = b)
			{
				ptr[21312412312] = 12;
			}

			//SimpleSockets();
			//OneClientSync();
			//ManyClientsAsync();
			//StressTest();
			//SimplePipes();
			//GetTypePerformance();

			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}

		private static void GetTypePerformance()
		{
			const int num = 100000;
			var name = typeof (Program).AssemblyQualifiedName;

			var sw1 = new Stopwatch();
			sw1.Start();
			for (int i = 0; i < num; ++i)
			{
				Type.GetType(name);
			}
			sw1.Stop();

			var values = new Dictionary<string,Type>();

			var sw2 = new Stopwatch();
			sw2.Start();
			for (int i = 0; i < num; ++i)
			{
				lock (values)
				{
					Type type;
					if (!values.TryGetValue(name, out type))
					{
						type = Type.GetType(name);
						values.Add(name, type);
					}
				}
			}
			sw2.Stop();

			var values2 = new ConcurrentDictionary<string, Type>();
			var sw3 = new Stopwatch();
			sw3.Start();
			for (int i = 0; i < num; ++i)
			{
				values2.GetOrAdd(name, Type.GetType);
			}
			sw3.Stop();

			Console.WriteLine("Type.GetType(string): {0}μs/type", 1.0 * sw1.Elapsed.Ticks*10 / num);
			Console.WriteLine("Dictionary.TryGetValue: {0}μs/type", 1.0 * sw2.Elapsed.Ticks * 10 / num);
			Console.WriteLine("ConcurrentyDictionary.GetOrAdd: {0}μs/type", 1.0 * sw3.Elapsed.Ticks * 10 / num);
		}

		private static void SimplePipes()
		{
			const string pipeName = "Klondyke bar";

			var time = TimeSpan.FromSeconds(5);
			const int messageLength = 90;
			long num = 0;

			var clientTask = new Task(() =>
				{
					using (var client = new NamedPipeClientStream(pipeName))
					{
						client.Connect();

						var reader = new BinaryReader(client);
						var writer = new BinaryWriter(client);

						var stopwatch = new Stopwatch();
						var buffer = new byte[messageLength];
						stopwatch.Start();
						while (stopwatch.Elapsed < time)
						{
							writer.Write(buffer);
							reader.Read(buffer, 0, buffer.Length);

							++num;
						}
						stopwatch.Stop();
					}
				});

			var serverTask = new Task(() =>
				{
					using (var server = new NamedPipeServerStream(pipeName,
						PipeDirection.InOut,
						1,
						PipeTransmissionMode.Message,
						PipeOptions.WriteThrough))
					{
						server.WaitForConnection();

						var reader = new BinaryReader(server);
						var writer = new BinaryWriter(server);

						var buffer = new byte[messageLength];
						try
						{
							while (server.IsConnected)
							{
								reader.Read(buffer, 0, buffer.Length);
								writer.Write(buffer);
							}
						}
						catch (IOException)
						{

						}
					}
				});

			clientTask.Start();
			serverTask.Start();

			clientTask.Wait();
			serverTask.Wait();

			var numSeconds = 5;
			var ops = 1.0 * num / numSeconds;
			Console.WriteLine("Total calls: {0}", num);
			Console.WriteLine("OP/s: {0:F2}k/s", ops / 1000);
			Console.WriteLine("Latency: {0}ns", time.Ticks * 100 / num);
		}

		private static void SimpleSockets()
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

		private static void StressTest()
		{
			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();

				const int desiredSteps = 1000000000;
				var numCorruptions = 0;

				var worker = silo.CreateGrain<IWorker, Worker>();
				var rng = new Random();
				var data = new byte[8];

				try
				{
					for (long i = 0; i < desiredSteps; ++i)
					{
						rng.NextBytes(data);
						var nextValue = BitConverter.ToInt64(data, 0);
						var actualValue = worker.Work(nextValue);
						if (actualValue != (~nextValue))
						{
							++numCorruptions;
						}

						if (i % 10000 == 0)
						{
							Console.WriteLine("{0}k calls", i / 1000);
						}
					}

					if (numCorruptions == 0)
					{
						Console.WriteLine("Test passed!");
					}
					else
					{
						Console.WriteLine("Test failed, {0} corruptions", numCorruptions);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("Test failed: {0}", e);
				}

				Console.WriteLine("{0:F2} m calls", desiredSteps/1000000.0);
				Console.WriteLine("{0:F2} Gb sent", 1.0*silo.NumBytesSent/1024/1024/1024);
				Console.WriteLine("{0:F2} Gb received", 1.0*silo.NumBytesReceived / 1024/1024/1024);
				Console.WriteLine("{0:F1} s in GC", silo.GarbageCollectionTime.TotalSeconds);
			}
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
