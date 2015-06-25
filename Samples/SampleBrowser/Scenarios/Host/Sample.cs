using System;

namespace SampleBrowser.Scenarios.Host
{
	public sealed class Sample
		: ISample
	{
		private bool _called;

		public void Call(string message)
		{
			_called = true;
			Console.WriteLine(message);
		}

		public string HaveYouBeenCalledYet()
		{
			return _called
				       ? "Ofcourse :)"
				       : "Not yet, unfortunately :(";
		}

		public void WritePi()
		{
			Console.WriteLine("PI is ~{0}", Math.PI);
		}
	}
}