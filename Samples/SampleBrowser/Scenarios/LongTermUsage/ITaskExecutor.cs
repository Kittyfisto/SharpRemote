using SharpRemote;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	public interface ITaskExecutor
	{
		[Invoke(Dispatch.SerializePerObject)]
		ITaskController Create(int? numDataPackets);

		[Invoke(Dispatch.SerializePerObject)]
		void Remove(ITaskController controller);
	}
}