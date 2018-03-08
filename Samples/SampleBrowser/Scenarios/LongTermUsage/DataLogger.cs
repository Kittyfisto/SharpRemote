using System;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	public sealed class DataLogger
	: IDataListener
	{
		public void Process(object data)
		{
			Console.WriteLine(data);
		}
	}
}
