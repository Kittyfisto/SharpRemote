using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Test.Types.Classes;

namespace SharpRemote.Test
{
	[TestFixture]
	public sealed class WeakKeyDictionaryTest
	{
		private static void EnsureIntegrity<TKey, TValue>(WeakKeyDictionary<TKey, TValue> dictionary) where TKey : class
		{
			var entries = new HashSet<int>();

			#region Free list

			if (dictionary._freeList >= 0)
			{
				// The length of the free list is bounded by the number of entries - this way we
				// can make sure that the freelist doesn't contain a loop
				int idx = dictionary._freeList;
				int count = 0;
				for (; idx != -1 && count < dictionary._entries.Length; ++count)
				{
					entries.Add(idx);

					WeakKeyDictionary<TKey, TValue>.Entry entry = dictionary._entries[idx];
					entry.HashCode.Should().Be(-1);
					entry.Key.Should().BeNull();
					entry.Value.Should().Be(default(TValue));

					idx = entry.Next;
					idx.Should().BeGreaterOrEqualTo(-1);
				}

				idx.Should().Be(-1);
				count.Should().Be(dictionary._freeCount);
			}
			else
			{
				dictionary._freeList.Should().Be(-1);
			}

			#endregion

			int numNonFreeBuckets = 0;
			for (int bucketIndex = 0; bucketIndex < dictionary._buckets.Length; ++bucketIndex)
			{
				int idx = dictionary._buckets[bucketIndex];
				if (idx >= 0)
				{
					entries.Add(idx).Should()
					                       .BeTrue(
						                       "because a root entry should neither be referenced by the free list, nor by any other bucket");

					++numNonFreeBuckets;

					WeakKeyDictionary<TKey, TValue>.Entry root = dictionary._entries[idx];
					root.HashCode.Should().NotBe(-1);
					root.Key.Should().NotBeNull();

					for (int i = root.Next; i != -1; i = dictionary._entries[i].Next)
					{
						entries.Add(i).Should()
										   .BeTrue(
											   "because no entry should be referenced twice");
						++numNonFreeBuckets;

						WeakKeyDictionary<TKey, TValue>.Entry entry = dictionary._entries[i];
						entry.HashCode.Should().NotBe(-1);
						root.Key.Should().NotBeNull();
					}
				}
				else
				{
					idx.Should().Be(-1);
				}
			}

			numNonFreeBuckets.Should().Be(dictionary.Count);
		}

		[Test]
		[Description("Verifies that adding one key-value pair works")]
		public void TestAdd1()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Add("Foobar", 42);
			dictionary.Count.Should().Be(1);
			dictionary.ContainsKey("Foobar").Should().BeTrue();
			dictionary.Version.Should().Be(1);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that adding two different keys works")]
		public void TestAdd2()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Add("Foobar", 42);
			dictionary.Add("Klondyke Bar", 9001);
			dictionary.Count.Should().Be(2);
			dictionary.ContainsKey("Foobar").Should().BeTrue();
			dictionary.ContainsKey("Klondyke Bar").Should().BeTrue();
			dictionary.Version.Should().Be(2);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that adding the same key twice is not allowed and throws an exception")]
		public void TestAdd3()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Add("Foobar", 42);
			new Action(() => dictionary.Add("Foobar", 42))
				.ShouldThrow<ArgumentException>()
				.WithMessage("An item with the same key has already been added.");

			dictionary.Count.Should().Be(1);
			dictionary.ContainsKey("Foobar").Should().BeTrue();
			dictionary.Version.Should().Be(1);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that after a resize, the dictionary is still able to find all previously added values")]
		public void TestAdd4()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Capacity.Should().Be(3);

			dictionary.Add("a", 1);
			dictionary.Add("b", 2);
			dictionary.Add("c", 3);
			dictionary.Capacity.Should().Be(3);

			dictionary.Add("d", 4);
			dictionary.Capacity.Should().BeInRange(3, 7);

			dictionary.Count.Should().Be(4);
			dictionary["a"].Should().Be(1);
			dictionary["b"].Should().Be(2);
			dictionary["c"].Should().Be(3);
			dictionary["d"].Should().Be(4);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that adding two keys with the same hash-code is allowed if they're not equal")]
		public void TestAdd5()
		{
			var dictionary = new WeakKeyDictionary<Key, int>();
			var key1 = new Key(1, 42);
			var key2 = new Key(2, 42);
			key1.GetHashCode().Should().Be(key2.GetHashCode());
			key1.Equals(key2).Should().BeFalse();

			dictionary.Add(key1, 9);
			dictionary.Add(key2, 10);
			dictionary.Count.Should().Be(2);
			dictionary.ContainsKey(key1).Should().BeTrue();
			dictionary.ContainsKey(key2).Should().BeTrue();

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description(
			"Verifies that adding a key to a full (count==capacity) dictionary doesn't cause a reallocation when an existing key with the same hashcode has expired"
			)]
		public void TestAdd6()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			var key1 = new Key(1, 42);
			WeakReference<Key> weakKey2 = null;
			var key3 = new Key(3, 42);

			new Action(() =>
				{
					var key2 = new Key(2, 42);
					weakKey2 = new WeakReference<Key>(key2);
					dictionary.Add(key1, 1);
					dictionary.Add(key2, 2);
					dictionary.Add(key3, 3);
				})();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			dictionary.Count.Should().Be(3);
			dictionary.Capacity.Should().Be(3);
			dictionary.Version.Should().Be(3);

			weakKey2.Should().NotBeNull();
			Key currentKey2;
			weakKey2.TryGetTarget(out currentKey2).Should().BeFalse("Because the key no longer exists");

			var key4 = new Key(4, 42);
			dictionary.Add(key4, 4);
			dictionary.Count.Should().Be(3);
			dictionary.Capacity.Should().Be(3);
			dictionary.Version.Should().Be(4);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that adding many items with pairwise different hash codes works")]
		public void TestAdd7()
		{
			const int numValues = 1000;
			var values = new List<object>(numValues);
			var dictionary = new WeakKeyDictionary<object, int>();

			for (int i = 0; i < numValues; ++i)
			{
				var key = new Key(i, i);
				values.Add(key);
				dictionary.Add(key, i);

				EnsureIntegrity(dictionary);
			}

			dictionary.Count.Should().Be(numValues);
			for(int i = 0; i < numValues; ++i)
			{
				var key = values[i];
				dictionary.ContainsKey(key).Should().BeTrue();
				dictionary[key].Should().Be(i);
			}
		}

		[Test]
		[Description("Verifies that the insertion performance is comparable to an ordinary dictionary")]
		public void TestAddPerformance()
		{
			const int num = 1000000;

			var keys = Enumerable.Range(0, num).Select(i => new object()).ToList();
			var dictionary = new Dictionary<object, int>();
			var weakDictionary = new WeakKeyDictionary<object, int>();

			var sw1 = new Stopwatch();
			sw1.Start();
			for (int i = 0; i < num; ++i)
			{
				dictionary.Add(keys[i], i);
			}
			sw1.Stop();

			var sw2 = new Stopwatch();
			sw2.Start();
			for (int i = 0; i < num; ++i)
			{
				weakDictionary.Add(keys[i], i);
			}
			sw2.Stop();

			Console.WriteLine("Dictionary.Add: {0}ms", sw1.ElapsedMilliseconds);
			Console.WriteLine("WeakKeyDictionary.Add: {0}ms", sw2.ElapsedMilliseconds);

			sw2.ElapsedMilliseconds.Should().BeLessOrEqualTo(sw1.ElapsedMilliseconds*10,
				"Because the weak key dictionary should not be slower than by an order of magnitude");
		}

		[Test]
		[Description("Verifies that the retrieval performance is comparable to an ordinary dictionary")]
		public void TestTryGetValuePerformance()
		{
			const int num = 100000;

			var keys = Enumerable.Range(0, num).Select(i => new object()).ToList();
			var dictionary = new Dictionary<object, int>();
			var weakDictionary = new WeakKeyDictionary<object, int>();

			for (int i = 0; i < num; ++i)
			{
				dictionary.Add(keys[i], i);
				weakDictionary.Add(keys[i], i);
			}

			var sw1 = new Stopwatch();
			sw1.Start();
			for (int i = 0; i < num; ++i)
			{
				var unused = dictionary[keys[i]];
			}
			sw1.Stop();

			var sw2 = new Stopwatch();
			sw2.Start();
			for (int i = 0; i < num; ++i)
			{
				var unused = weakDictionary[keys[i]];
			}
			sw2.Stop();

			Console.WriteLine("Dictionary[]: {0}ms", sw1.ElapsedMilliseconds);
			Console.WriteLine("WeakKeyDictionary[]: {0}ms", sw2.ElapsedMilliseconds);

			sw2.ElapsedMilliseconds.Should().BeLessOrEqualTo(sw1.ElapsedMilliseconds * 10,
				"Because the weak key dictionary should not be slower than by an order of magnitude");
		}

		[Test]
		[Description("Verifies that Collect() reclaims entries which's keys no longer exist")]
		public void TestCollect1()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			WeakReference<object> weakKey = null;
			new Action(() =>
				{
					var key = new ByReferenceClass();
					weakKey = new WeakReference<object>(key);

					dictionary.Add(key, 42);
					dictionary.Count.Should().Be(1);
				})();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			weakKey.Should().NotBeNull();

			dictionary.Collect();
			dictionary.Count.Should().Be(0);
			object unused;
			weakKey.TryGetTarget(out unused).Should().BeFalse("Because the dictionary shouldn't keep the key alive");

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that Collect() correctly reclaims all three keys from the same bucket")]
		public void TestCollect2()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			new Action(() =>
			{
				dictionary.Add(new Key(1, 9001), 1);
				dictionary.Add(new Key(2, 9001), 2);
				dictionary.Add(new Key(3, 9001), 3);
			})();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			dictionary.Count.Should().Be(3);
			dictionary.Collect();
			dictionary.Count.Should().Be(0);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that Collect() correctly reclaims 2 of the three keys from the same bucket")]
		public void TestCollect3()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			var key3 = new Key(3, 9001);
			new Action(() =>
			{
				dictionary.Add(new Key(1, 9001), 1);
				dictionary.Add(new Key(2, 9001), 2);
				dictionary.Add(key3, 3);
			})();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			dictionary.Count.Should().Be(3);
			dictionary.Collect();
			dictionary.Count.Should().Be(1);
			dictionary[key3].Should().Be(3);
			dictionary._freeCount.Should().Be(2, "Because 2 previously used entries were reclaimed");

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that Collect() correctly reclaims 2 of the four keys from the same bucket")]
		public void TestCollect4()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			var key1 = new Key(1, 9001);
			var key4 = new Key(4, 9001);
			new Action(() =>
			{
				dictionary.Add(key1, 1);
				dictionary.Add(new Key(2, 9001), 2);
				dictionary.Add(new Key(3, 9001), 3);
				dictionary.Add(key4, 4);
			})();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			dictionary.Count.Should().Be(4);
			dictionary.Collect();
			dictionary.Count.Should().Be(2);
			dictionary[key1].Should().Be(1);
			dictionary[key4].Should().Be(4);
			dictionary._freeCount.Should().Be(2, "Because 2 previously used entries were reclaimed");

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that Collect() doesn't do anything on an empty dictionary")]
		public void TestCollect5()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			dictionary.Version.Should().Be(0);
			dictionary.Collect();
			dictionary.Version.Should().Be(0, "Because no entry should've been modified");

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that Collect() doesn't do anything on keys that have not been collected by the GC")]
		public void TestCollect6()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			var key1 = new object();
			var key2 = new object();

			dictionary.Add(key1, 1337);
			dictionary.Add(key2, 42);
			dictionary.Version.Should().Be(2);
			dictionary.Count.Should().Be(2);

			dictionary.Collect();
			dictionary.Version.Should().Be(2);
			dictionary.Count.Should().Be(2);
			dictionary[key1].Should().Be(1337);
			dictionary[key2].Should().Be(42);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that Collect() correctly reclaims 2 keys from different buckets")]
		public void TestCollect7()
		{
			var dictionary = new WeakKeyDictionary<object, int>();
			new Action(() =>
			{
				dictionary.Add(new Key(1, 42), 1);
				dictionary.Add(new Key(2, 9001), 2);
			})();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			dictionary.Count.Should().Be(2);
			dictionary.Collect();
			dictionary.Count.Should().Be(0);
			dictionary._freeCount.Should().Be(2, "Because 2 previously used entries were reclaimed");

			EnsureIntegrity(dictionary);
		}

		[Test]
		public void TestCtor()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Count.Should().Be(0);
			dictionary.Version.Should().Be(0);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that the value can also be retrieved via the indexer")]
		public void TestIndexer1()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Add("a", 1);
			dictionary["a"].Should().Be(1);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that accessing a non-added key via the indexer throws")]
		public void TestIndexer2()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Add("a", 1);
			new Action(() => { int value = dictionary["b"]; })
				.ShouldThrow<ArgumentException>()
				.WithMessage("The given key was not present in the dictionary.");

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that TryGetValue returns the value if it exists")]
		public void TestTryGetValue1()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			dictionary.Add("1", 42);
			dictionary.Add("2", 9001);
			dictionary.Add("3", 1337);

			int value;

			dictionary.TryGetValue("1", out value).Should().BeTrue();
			value.Should().Be(42);
			dictionary.TryGetValue("2", out value).Should().BeTrue();
			value.Should().Be(9001);
			dictionary.TryGetValue("3", out value).Should().BeTrue();
			value.Should().Be(1337);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that TryGetValue returns false when the key doesn't exist")]
		public void TestTryGetValue2()
		{
			var dictionary = new WeakKeyDictionary<string, int>();
			int value;
			dictionary.TryGetValue("1", out value).Should().BeFalse();
			value.Should().Be(0);

			dictionary.Add("1", 42);

			dictionary.TryGetValue("2", out value).Should().BeFalse();
			value.Should().Be(0);

			EnsureIntegrity(dictionary);
		}

		[Test]
		[Description("Verifies that TryGetValue doesn't modify the dictionary")]
		public void TestTryGetValue3()
		{
			var dictionary = new WeakKeyDictionary<string, object>();
			dictionary.Version.Should().Be(0);

			object value;
			dictionary.TryGetValue("foo", out value);

			dictionary.Count.Should().Be(0);
			dictionary.Version.Should().Be(0, "Because the dictionary shouldn't have changed");

			EnsureIntegrity(dictionary);
		}
	}
}