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

		public int Length => _values.Length;

		public T Enqueue(T value)
		{
			var previous = _values[_head];
			_values[_head++] = value;
			_head %= _values.Length;
			return previous;
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