using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace SharpRemote
{
	/// <summary>
	/// A dictionary that maps keys to values. Contrary to <see cref="Dictionary{TKey, TValue}"/>
	/// keys are stored in a <see cref="WeakReference"/> and thus entries in this dictionary
	/// will automatically be removed (No longer visible)
	/// </summary>
	internal sealed class WeakKeyDictionary<TKey, TValue>
		: IDisposable
		where TKey : class
	{
		private const int HashCodeMask = 0x7FFFFFFF;

		internal struct Entry
		{
			public int HashCode;  // Lower 31 bits of hash code, -1 if unused
			public int Next;      // Index of next entry, -1 if last
			public GCHandle Key;  // Key of entry
			public TValue Value;  // Value of entry
		}

// ReSharper disable InconsistentNaming
		internal readonly IEqualityComparer<TKey> _comparer;
		internal int[] _buckets;
		internal Entry[] _entries;
		internal int _freeList;
		internal int _freeCount;
		internal int _version;
		internal int _count;
		private bool _disposed;
// ReSharper restore InconsistentNaming

		public int Count
		{
			get { return _count - _freeCount; }
		}

		internal int Version
		{
			get { return _version; }
		}

		public WeakKeyDictionary()
		{
			_comparer = EqualityComparer<TKey>.Default;

			Initialize(0);
		}

		~WeakKeyDictionary()
		{
			Clear();
		}

		public override string ToString()
		{
			return string.Format("Count: {0}", Count);
		}

		private void Initialize(int capacity)
		{
			int size = HashHelpers.GetPrime(capacity);
			_buckets = new int[size];
			for (int i = 0; i < _buckets.Length; i++) _buckets[i] = -1;
			_entries = new Entry[size];
			_freeList = -1;
		}

		/// <summary>
		/// Adds the given key-value pair to this dictionary.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		public void Add(TKey key, TValue value)
		{
			Insert(key, value, true);
		}

		/// <summary>
		/// Tests if the given key is part of this dictionary and returns true if it is, false otherwise.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public bool ContainsKey(TKey key)
		{
			return FindEntry(key) >= 0;
		}

		private static bool TryGetTarget(GCHandle handle, out TKey target)
		{
			if (handle.IsAllocated)
			{
				var key = handle.Target;
				target = key as TKey;
				return target != null;
			}

			target = null;
			return false;
		}

		/// <summary>
		/// Finds the first occurence of the given key and returns its index, or -1 if it doesn't exist.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private int FindEntry(TKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			if (_buckets != null)
			{
				int hashCode = _comparer.GetHashCode(key) & HashCodeMask;
				for (int i = _buckets[hashCode % _buckets.Length]; i >= 0; i = _entries[i].Next)
				{
					TKey actualKey;
					if (_entries[i].HashCode == hashCode &&
						TryGetTarget(_entries[i].Key, out actualKey) &&
						_comparer.Equals(actualKey, key))
						return i;
				}
			}
			return -1;
		}

		private void Insert(TKey key, TValue value, bool add)
		{
			if (key == null)
				throw new ArgumentNullException("key");

			//if (_buckets == null) Initialize(0);
			var hashCode = key.GetHashCode() & HashCodeMask;
			var targetBucket = hashCode % _buckets.Length;
			int previousBucket = -1;

			for (int i = _buckets[targetBucket]; i >= 0; i = _entries[i].Next)
			{
				TKey storedKey;
				if (TryGetTarget(_entries[i].Key, out storedKey))
				{
					if (_entries[i].HashCode == hashCode)
					{
						if (_comparer.Equals(storedKey, key))
						{
							if (add)
							{
								throw new ArgumentException("An item with the same key has already been added.");
							}

							_entries[i].Value = value;
							_version++;
							return;
						}
					}
				}
				else
				{
					// We have found an entry in the list of buckets that is no longer in use
					// because it's key has been collected. This means we can put this bucket into
					// the free list
					if (previousBucket != -1)
					{
						_entries[previousBucket].Next = _entries[i].Next;

						// Now that this bucket has been removed from the list we can
						// insert it into the front of the free list.
						_entries[i].Next = _freeList;
						Free(ref _entries[i].Key);
						_entries[i].Value = default(TValue);
						_entries[i].HashCode = -1;

						_freeList = i;
						++_freeCount;
						++_version;

						i = previousBucket;
					}
				}

				previousBucket = i;
			}

			int index;
			if (_freeCount > 0)
			{
				index = _freeList;
				_freeList = _entries[index].Next;
				_freeCount--;
			}
			else
			{
				if (_count == _entries.Length)
				{
					Resize();
					targetBucket = hashCode % _buckets.Length;
				}
				index = _count;
				_count++;
			}

			_entries[index].HashCode = hashCode;
			_entries[index].Next = _buckets[targetBucket];
			_entries[index].Key = GCHandle.Alloc(key, GCHandleType.Weak);
			_entries[index].Value = value;
			_buckets[targetBucket] = index;
			_version++;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			int i = FindEntry(key);
			if (i >= 0)
			{
				value = _entries[i].Value;
				return true;
			}
			value = default(TValue);
			return false;
		}

		public TValue this[TKey key]
		{
			get
			{
				int i = FindEntry(key);
				if (i >= 0) return _entries[i].Value;
				throw new ArgumentException("The given key was not present in the dictionary.");
			}
			set
			{
				Insert(key, value, false);
			}
		}

		public int Capacity
		{
			get { return _buckets.Length; }
		}

		private void Resize()
		{
			Resize(HashHelpers.ExpandPrime(_count));
		}

		private void Resize(int newSize)
		{
			Contract.Assert(newSize >= _entries.Length);

			var newBuckets = new int[newSize];
			for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
			var newEntries = new Entry[newSize];
			Array.Copy(_entries, 0, newEntries, 0, _count);

			for (int i = 0; i < _count; i++)
			{
				if (newEntries[i].HashCode >= 0)
				{
					int bucket = newEntries[i].HashCode % newSize;
					newEntries[i].Next = newBuckets[bucket];
					newBuckets[bucket] = i;
				}
			}
			_buckets = newBuckets;
			_entries = newEntries;
		}

		public bool Remove(TKey key)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}

			if (_buckets != null)
			{
				int hashCode = _comparer.GetHashCode(key) & 0x7FFFFFFF;
				int bucket = hashCode % _buckets.Length;
				int last = -1;
				for (int i = _buckets[bucket]; i >= 0; last = i, i = _entries[i].Next)
				{
					if (_entries[i].HashCode == hashCode)
					{
						TKey actualKey;
						if (TryGetTarget(_entries[i].Key, out actualKey))
						{
							if (_comparer.Equals(actualKey, key))
							{
								if (last < 0)
								{
									_buckets[bucket] = _entries[i].Next;
								}
								else
								{
									_entries[last].Next = _entries[i].Next;
								}
								_entries[i].HashCode = -1;
								_entries[i].Next = _freeList;
								Free(ref _entries[i].Key);
								_entries[i].Value = default(TValue);
								_freeList = i;
								_freeCount++;
								_version++;
								return true;
							}
						}
					}
				}
			}
			return false;
		}

		private static void Free(ref GCHandle key)
		{
			if (key.IsAllocated)
				key.Free();
		}

		public List<TValue> Collect(bool returnCollectedValues = false)
		{
			List<TValue> collectedValues = null;

			if (_buckets != null)
			{
				for (int bucketIndex = 0; bucketIndex < _buckets.Length; ++bucketIndex)
				{
					int lastValidEntryIndex = -1;
					for (int i = _buckets[bucketIndex]; i != -1;)
					{
						TKey unused;
						if (!TryGetTarget(_entries[i].Key, out unused))
						{
							int nextEntry;
							if (i == _buckets[bucketIndex])
							{
								// We may need to update the index in the bucket list in case we just
								// collected the first entry in the linked list and set the index
								// to the next entry.
								_buckets[bucketIndex] = nextEntry = _entries[i].Next;
							}
							else
							{
								// If we didn't reclaim the first bucket then we reclaimed some other bucket
								// in the middle of the linked list and thus we must patch the next index
								// from the previous index instead
								_entries[lastValidEntryIndex].Next = nextEntry = _entries[i].Next;
							}

							if (returnCollectedValues)
							{
								if (collectedValues == null)
									collectedValues = new List<TValue>();

								collectedValues.Add(_entries[i].Value);
							}

							// This entry can be reclaimed because it's key is no longer alive
							_entries[i].HashCode = -1;
							_entries[i].Next = _freeList;
							Free(ref _entries[i].Key);
							_entries[i].Value = default(TValue);
							_freeList = i;
							_freeCount++;
							_version++;

							i = nextEntry;
						}
						else
						{
							lastValidEntryIndex = i;
							i = _entries[i].Next;
						}
					}
				}
			}

			return collectedValues;
		}

		public void Clear()
		{
			for (int bucketIndex = 0; bucketIndex < _buckets.Length; ++bucketIndex)
			{
				for (int i = _buckets[bucketIndex]; i != -1; i = _entries[i].Next)
				{
					Free(ref _entries[i].Key);
				}
			}
			Initialize(0);
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				Clear();

				GC.SuppressFinalize(this);
				_disposed = true;
			}
		}
	}
}