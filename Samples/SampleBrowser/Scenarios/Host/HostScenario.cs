using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

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

		public ICommand RunTestCommand
		{
			get { return new TaskCommand(() => Task.Factory.StartNew(RunTest)); }
		}

		private void RunTest()
		{
		}

		public override FrameworkElement CreateView()
		{
			return new HostView();
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