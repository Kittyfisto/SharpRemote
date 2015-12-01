namespace SharpRemote
{
	internal sealed class Buffer<T>
	{
		private readonly T[] _data;
		private int _head;

		public Buffer(int count)
		{
			_data = new T[count];
			_head = 0;
		}

		public void Push(T value)
		{
			_data[_head++] = value;
		}

		public T Get()
		{
			return _data[_head--];
		}
	}
}