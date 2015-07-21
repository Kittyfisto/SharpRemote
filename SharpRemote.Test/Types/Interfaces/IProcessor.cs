namespace SharpRemote.Test.Types.Interfaces
{
	public interface IProcessor
	{
		void Process();

		[Invoke(Dispatch.SerializePerObject)]
		void Report(string message);

		[Invoke(Dispatch.SerializePerObject)]
		void AddListener(IListener listener);

		[Invoke(Dispatch.SerializePerObject)]
		void RemoveListener(IListener listener);
	}
}