using System.Threading.Tasks;
using System.Windows;
using SharpRemote.Hosting;

namespace SampleBrowser.Scenarios.Host
{
	public sealed class HostScenario
		: AbstractScenario
	{
		public HostScenario()
			: base("Host",
			       "Start and connect to a host application on the same computer and run a test suite over the network")
		{
		}

		public override FrameworkElement CreateView()
		{
			return new HostView();
		}

		protected override void RunTest()
		{
			using (var appender = new LogInterceptor(Log))
			using (var silo = new ProcessSilo())
			{
				
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