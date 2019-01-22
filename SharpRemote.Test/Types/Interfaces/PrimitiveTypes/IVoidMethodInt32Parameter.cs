using System;

namespace SharpRemote.Test.Types.Interfaces.PrimitiveTypes
{
	[ByReference]
	public interface IVoidMethodInt32Parameter
	{
		void DoStuff(Int32 value);
	}
}