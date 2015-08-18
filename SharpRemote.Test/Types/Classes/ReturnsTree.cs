using SharpRemote.Test.Types.Interfaces.NativeTypes;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class ReturnsTree
		: IReturnsObjectMethod
	{
		public object GetListener()
		{
			return new Tree();
		}
	}
}