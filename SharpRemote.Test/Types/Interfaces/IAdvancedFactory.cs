using System;

namespace SharpRemote.Test.Types.Interfaces
{
	[ByReference]
	public interface IAdvancedFactory
	{
		object Create(Type type);
	}
}