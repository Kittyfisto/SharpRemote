using System.IO;
using System.Threading;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Config;

namespace ConsoleApplication1
{
	internal class Program
	{
		private static unsafe void Main(string[] args)
		{
			XmlConfigurator.Configure(new FileInfo("ConsoleApplication1.exe.config"));

			using (var silo = new OutOfProcessSilo())
			{
				silo.Start();
				var id = silo.HostProcessId;
				var something = silo.CreateGrain<IInt32Method, Returns42>();

				var threads = new Thread[4];
				for (int i = 0; i < 4; ++i)
				{
					threads[i] = new Thread(() => DoStuff(something));
					threads[i].Start();
				}

				foreach (var thread in threads)
				{
					thread.Join();
				}
			}
		}

		private static void DoStuff(IInt32Method something)
		{
			while (true)
			{
				something.Do();
			}
		}
	}
}
