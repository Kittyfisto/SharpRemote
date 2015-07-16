using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	internal sealed class ReturnsNearlyInt64Max
		: IGetInt64Property
	{
		public long Value
		{
			get { return long.MaxValue-1; }
		}
	}
}