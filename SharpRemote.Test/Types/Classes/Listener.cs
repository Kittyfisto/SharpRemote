using System.Collections.Generic;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class Listener
		: IListener
	{
		public readonly List<string> Messages;

		public Listener()
		{
			Messages = new List<string>();
		}

		public void Report(string message)
		{
			Messages.Add(message);
		}
	}
}