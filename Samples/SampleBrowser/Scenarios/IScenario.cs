using System.Windows.Input;

namespace SampleBrowser.Scenarios
{
	public interface IScenario
	{
		string Title { get; }
		string Description { get; }

		ICommand StartCommand { get; }
		ICommand StopCommand { get; }
		void Stop();
	}
}