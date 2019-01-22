using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class Returns42
		: IGetInt32Property
		, IInt32Method
	{
		public int Value
		{
			get { return 42; }
		}

		public int DoStuff()
		{
			return 42;
		}
	}
}