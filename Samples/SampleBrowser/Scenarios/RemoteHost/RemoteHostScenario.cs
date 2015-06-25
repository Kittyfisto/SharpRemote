using System.Threading.Tasks;
using System.Windows;

namespace SampleBrowser.Scenarios.RemoteHost
{
	public sealed class RemoteHostScenario
		: AbstractScenario
	{
		public RemoteHostScenario()
			: base(
			"Remote Host",
			"Contact a host application on another computer and run a test suite over the network"
			)
		{
			
		}

		public override FrameworkElement CreateView()
		{
			return null;
		}

		protected override Task Start()
		{
			return Task.Factory.StartNew(() =>
				{

				});
		}

		protected override Task Stop()
		{
			return Task.Factory.StartNew(() =>
			{

			});
		}
	}
}