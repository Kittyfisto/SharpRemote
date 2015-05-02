using System;

namespace SharpRemote.Test.CodeGeneration.Types.Interfaces
{
	public interface IEvent
	{
		event Action<int> Foobar;
	}
}