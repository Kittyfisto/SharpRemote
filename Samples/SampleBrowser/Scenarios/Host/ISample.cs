namespace SampleBrowser.Scenarios.Host
{
	public interface ISample
	{
		void Call(string message);
		string HaveYouBeenCalledYet();
		void WritePi();
	}
}