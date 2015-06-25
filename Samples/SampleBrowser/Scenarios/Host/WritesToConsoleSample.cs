using System;

namespace SampleBrowser.Scenarios.Host
{
	public sealed class WritesToConsoleSample
		: IWritesToConsoleSample
	{
		public void Write(string message)
		{
			Console.WriteLine(message);
		}
	}
}