using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	internal sealed class Returns42
		: IGetInt32Property
	{
		public int Value
		{
			get { return 42; }
		}
	}
}