using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class ByReferenceClass
		: IByReferenceType
	{
		public int HashCode
		{
			get { return GetHashCode(); }
		}
	}
}