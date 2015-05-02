using System;
using SharpRemote.Test.CodeGeneration.Types.Interfaces;

namespace SharpRemote.Test.CodeGeneration
{
	public class Test
		: IEvent
	{
		public void Invoke2(int n)
		{
			var fn = Foobar;
			if (fn != null)
				fn(n);
		}

		public event Action<int> Foobar;
	}
}