namespace SharpRemote.Test.Types.Interfaces
{
	[ByReference]
	public interface IListener
	{
		void Report(string message);
	}
}