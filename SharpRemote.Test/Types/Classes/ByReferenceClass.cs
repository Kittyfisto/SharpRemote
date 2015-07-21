using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class ByReferenceClass
		: IByReferenceType
	{
		private readonly int _value;

		public ByReferenceClass(int value = 0)
		{
			_value = value;
		}

		public int Value
		{
			get { return _value; }
		}
	}
}