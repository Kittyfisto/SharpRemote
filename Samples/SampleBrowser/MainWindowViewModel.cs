using SampleBrowser.Scenarios;

namespace SampleBrowser
{
	public sealed class MainWindowViewModel
	{
		private readonly IScenario[] _scenarios;

		public MainWindowViewModel()
		{
			_scenarios = new IScenario[]
				{
					new RemoteHostScenario()
				};
		}

		public IScenario[] Scenarios
		{
			get { return _scenarios; }
		}
	}
}