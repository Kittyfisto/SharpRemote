using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	internal sealed class ByReferenceType
		: IByReferenceType
	{
		public int HashCode
		{
			get { return GetHashCode(); }
		}
	}
}