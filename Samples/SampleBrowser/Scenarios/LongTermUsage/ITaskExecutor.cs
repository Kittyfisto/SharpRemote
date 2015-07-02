using SharpRemote;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	public interface ITaskExecutor
	{
		[Invoke(Dispatch.OncePerObject)]
		ITaskController Create(int? numDataPackets);

		[Invoke(Dispatch.OncePerObject)]
		void Remove(ITaskController controller);
	}
}