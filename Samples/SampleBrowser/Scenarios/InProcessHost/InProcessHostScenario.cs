using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using SharpRemote;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;
using log4net.Core;

namespace SampleBrowser.Scenarios.InProcessHost
{
	internal sealed class InProcessHostScenario
		: AbstractScenario
	{
		public InProcessHostScenario()
			: base("In Proc Host",
			       "Start and connect to a host application on the same computer and run a test suite over the network")
		{
		}

		public override FrameworkElement CreateView()
		{
			return new InProcessHostView();
		}

		protected override bool RunTest()
		{
			using (var appender = new LogInterceptor(Log, Level.Info))
			using (var silo = new InProcessRemotingSilo())
			{
				//Ssilo.Start();
				var grain = silo.CreateGrain<IGetInt64Property, ReturnsNearlyInt64Max>();

				long sum = 0;
				long num = 0;
				var time = TimeSpan.FromSeconds(20);
				var watch = new Stopwatch();

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

				var numSeconds = watch.Elapsed.TotalSeconds;
				var ops = 1.0 * num / numSeconds;
				Log(string.Format("Total calls: {0} (sum: {1})", num, sum));
				Log(string.Format("OP/s: {0:F2}k/s", ops / 1000));

				return true;
			}
		}

		protected override Task Start()
		{
			return Task.Factory.StartNew(() => { });
		}

		protected override Task Stop()
		{
			return Task.Factory.StartNew(() => { });
		}
	}
}
