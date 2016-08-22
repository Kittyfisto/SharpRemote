using System;

namespace SharpRemote.Test.Types.Interfaces
{
	public interface IActionEventStringArray
	{
		event Action<string[]> Do;
	}
}