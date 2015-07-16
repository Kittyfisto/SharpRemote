using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	internal sealed class ReturnsInt64Max
		: IGetInt64Property
	{
		public long Value
		{
			get { return long.MaxValue; }
		}
	}
}