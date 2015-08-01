namespace SharpRemote.Test
{
	public sealed class Key
	{
		private readonly int _value;
		private readonly int _hashCode;

		public Key(int value, int hashCode)
		{
			_value = value;
			_hashCode = hashCode;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is Key))
				return false;

			var key = (Key) obj;
			return key._value == _value;
		}

		public override int GetHashCode()
		{
			return _hashCode;
		}
	}
}