using System.Threading.Tasks;
using SharpRemote;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	[ByReference]
	public interface ITaskController
	{
		Task ExecuteCommand(ICommandDescription command);

		void Start();
		bool IsRunning { get; }
		void Stop();

		void RegisterDataListener(IDataListener listener);
		void UnregisterDataListener(IDataListener listener);
	}
}