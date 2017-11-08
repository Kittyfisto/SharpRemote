using System.Collections;
using System.Collections.Generic;

namespace SharpRemote
{
	internal sealed class RingBuffer<T>
		: IEnumerable<T>
	{
		private readonly T[] _values;
		private int _head;

		public RingBuffer(int length)
		{
			_values = new T[length];
		}

		public int Length
		{
			get { return _values.Length; }
		}

		public void Enqueue(T value)
		{
			_values[_head++] = value;
			_head %= _values.Length;
		}

		public override string ToString()
		{
			return string.Format("Count: {0}, Head: {1}", _values.Length, _head);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return ((IEnumerable<T>)_values).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}