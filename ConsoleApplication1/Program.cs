using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.Hosting;
using SharpRemote.Hosting.OutOfProcess;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Config;
using SharpRemote.Test;

namespace ConsoleApplication1
{
	class StateMachine
	{
		private Task<int> _subjectTask;
		private TaskCompletionSource<int> _source;

		public void OnCompleted()
		{
			try
			{
				if (_subjectTask.Exception != null)
				{
					InvokeFallback();
				}
				else
				{
					_source.SetResult(_subjectTask.Result);
				}
			}
			catch (Exception)
			{
				InvokeFallback();
			}
		}

		private void InvokeFallback()
		{

		}
	}

	internal class Program
	{
		private static unsafe void Main(string[] args)
		{
			var task = TaskEx.Failed<int>(new FileNotFoundException());
			task.GetAwaiter().OnCompleted(SomeMethod);

			Thread.Sleep(1000);
			return;
			/*
			//Client
			var client = new NamedPipeClientStream("PipesOfPiece");
			client.Connect();
			var reader = new BinaryReader(client);
			var writer = new BinaryWriter(client);

			var data = new byte[64];
			int roundtrips = 0;
			var sw = new Stopwatch();
			while (true)
			{
				sw.Start();

				writer.Write(data.Length);
				writer.Write(data);
				writer.Flush();

				var length = reader.ReadInt32();
				var buffer = new byte[length];
				reader.Read(buffer, 0, length);

				sw.Stop();

				++roundtrips;

				if (roundtrips%10000 == 0)
				{
					var roundtripTime = sw.Elapsed.Ticks/roundtrips;
					Console.WriteLine("{0}μs rtt", (int)roundtripTime / 10);
				}
			}*/
		}

		private static void SomeMethod()
		{
			int n = 0;
		}

		static void StartServer()
		{
			Task.Factory.StartNew(() =>
			{
				var server = new NamedPipeServerStream("PipesOfPiece");
				server.WaitForConnection();
				var reader = new BinaryReader(server);
				var writer = new BinaryWriter(server);

				while (true)
				{
					var length = reader.ReadInt32();
					var buffer = new byte[length];
					reader.Read(buffer, 0, length);

					writer.Write(buffer.Length);
					writer.Write(buffer);
					writer.Flush();
				}
			});
		}

		private static void DoStuff(IInt32Method something)
		{
			while (true)
			{
				something.Do();
				Thread.Sleep(100);
			}
		}
	}
}
