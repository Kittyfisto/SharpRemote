using System;

namespace SharpRemote.Test.Types.Interfaces.NativeTypes
{
	public sealed class ReturnsTypeofString
		: IReturnsType
	{
		public Type Do()
		{
			return typeof (string);
		}
	}
}