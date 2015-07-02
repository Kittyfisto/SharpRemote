using System.Threading.Tasks;
using System.Windows;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	public sealed class LongTermScenario
		: AbstractScenario
	{
		public LongTermScenario()
			: base(
				"Long Term Usage",
				"Publishes an application to a (remote) watchdog and performs long-term real world tests"
				)
		{
		}

		public override FrameworkElement CreateView()
		{
			return new LongTermView();
		}

		protected override bool RunTest()
		{
			return false;
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