namespace ConsoleApplication1
{
	public interface IWorker
	{
		void RegisterListener(IDataListener listener);
		void Start();
		void Stop();
	}
}