namespace SharpRemote.Test.Types.Interfaces
{
	public interface IProcessor
	{
		void Process();
		void Report(string message);

		void AddListener(IListener listener);

		void RemoveListener(IListener listener);
	}
}