using System;
using System.Threading;

namespace SharpRemote
{
	internal sealed class BlockingQueue<T>
		: IDisposable
	{
		private readonly CancellationTokenSource _cancellationRequested;
		private readonly SemaphoreSlim _dequeueSemaphore;
		private readonly SemaphoreSlim _enqueueSemaphore;
		private readonly object _syncRoot;
		private readonly T[] _values;

		private int _count;
		private int _dequeueIndex;
		private int _enqueueIndex;

		public BlockingQueue(int maximumCapacity)
		{
			if (maximumCapacity <= 0)
				throw new ArgumentOutOfRangeException("maximumCapacity", "maximumCapacity must be greater than 0");

			_syncRoot = new object();
			_values = new T[maximumCapacity];
			_dequeueIndex = 0;
			_dequeueIndex = 0;
			_count = 0;
			_cancellationRequested = new CancellationTokenSource();

			_enqueueSemaphore = new SemaphoreSlim(maximumCapacity, maximumCapacity);
			_dequeueSemaphore = new SemaphoreSlim(0, maximumCapacity);
		}

		public T this[int index]
		{
			get { return _values[index]; }
		}

		public int Count
		{
			get { return _count; }
		}

		public void Dispose()
		{
			_cancellationRequested.Cancel();
			// We can't really dispose of the token because it might still be used...
			// The GC will take care of it..
		}

		/// <summary>
		///     Adds the given item, blocks if the maximum capacity has been reached until at least one
		///     item has been retrieved.
		/// </summary>
		/// <param name="value"></param>
		/// <exception cref="OperationCanceledException">When this collection has been disposed of</exception>
		public void Enqueue(T value)
		{
			_enqueueSemaphore.Wait(_cancellationRequested.Token);

			lock (_syncRoot)
			{
				_values[_enqueueIndex] = value;
				_enqueueIndex = (_enqueueIndex + 1)%_values.Length;
				_dequeueSemaphore.Release();
				++_count;
			}
		}

		/// <summary>
		///     Removes the first item from this collection.
		/// </summary>
		/// <returns>True when the item was removed, false when this collection was disposed</returns>
		public T Dequeue()
		{
			_dequeueSemaphore.Wait(_cancellationRequested.Token);

			lock (_syncRoot)
			{
				T value = _values[_dequeueIndex];
				_values[_dequeueIndex] = default(T);
				_dequeueIndex = (_dequeueIndex + 1)%_values.Length;
				_enqueueSemaphore.Release();
				--_count;
				return value;
			}
		}
	}
}