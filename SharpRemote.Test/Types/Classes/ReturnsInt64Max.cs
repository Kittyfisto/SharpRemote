using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class ReturnsInt64Max
		: IGetInt64Property
	{
		public long Value
		{
			get { return long.MaxValue; }
		}
	}
}