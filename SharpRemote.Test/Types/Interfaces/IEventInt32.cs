using System;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IEventInt32
	{
		event Action<int> Foobar;
	}
}