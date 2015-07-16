using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Types.Classes
{
	internal sealed class Returns9000
		: IGetInt16Property
	{
		public short Value
		{
			get { return 9000; }
		}
	}
}