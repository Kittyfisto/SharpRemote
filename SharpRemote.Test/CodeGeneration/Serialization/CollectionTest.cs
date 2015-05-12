using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestIntList()
		{
			_serializer.ShouldRoundtripEnumeration(new List<int> { 1, 2, 3, 4 });
			_serializer.ShouldRoundtripEnumeration(new List<int> { 1 });
			_serializer.ShouldRoundtripEnumeration(new List<int>());
			_serializer.ShouldRoundtripEnumeration(new List<int> {9001, int.MinValue, int.MaxValue});
		}

		[Test]
		public void TestIntHashSet()
		{
			_serializer.ShouldRoundtripEnumeration(new HashSet<int>());
			_serializer.ShouldRoundtripEnumeration(new HashSet<int> {1, 2, 3, 4});
		}

		[Test]
		public void TestStringHashSet()
		{
			var value = new HashSet<string>(
				Enumerable.Range(0, 10000)
				.Select(x => x.ToString(CultureInfo.InvariantCulture))
				);
			_serializer.ShouldRoundtripEnumeration(value);
		}

		[Test]
		public void TestObjectHashSet()
		{
			_serializer.ShouldRoundtripEnumeration(new HashSet<object>());
			_serializer.ShouldRoundtripEnumeration(new HashSet<object>
			{
				42,
				"Foobar",
				IPAddress.Parse("192.168.0.20")
			});
		}

		[Test]
		public void TestLinkedList()
		{
			_serializer.ShouldRoundtripEnumeration(new LinkedList<IPAddress>());

			var values = new LinkedList<IPAddress>();
			values.AddLast(IPAddress.Loopback);
			values.AddLast(IPAddress.IPv6Any);
			values.AddLast(IPAddress.IPv6Loopback);
			values.AddLast(IPAddress.IPv6None);
			values.AddLast(IPAddress.Any);
			values.AddLast(IPAddress.Broadcast);
			values.AddLast(IPAddress.None);

			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestSortedList()
		{
			_serializer.ShouldRoundtripEnumeration(new LinkedList<IPAddress>());

			var values = new SortedList<int, string>
			{
				{1, "Never happened"},
				{2, "Attach of the Clones"},
				{3, "Revenge of the Sith"},
				{4, "A new hope"},
				{5, "The empire strikes back"},
				{6, "Return of the jedi"},
			};
			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestStack()
		{
			_serializer.ShouldRoundtripEnumeration(new Stack<int>());

			var values = new Stack<int>();
			values.Push(1);
			values.Push(2);
			values.Push(4);
			values.Push(3);
			values.Push(5);
			values.Push(42);
			values.Push(9001);

			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestSortedDictionary()
		{
			_serializer.ShouldRoundtripEnumeration(new SortedDictionary<int, string>());
			_serializer.ShouldRoundtripEnumeration(new SortedDictionary<int, string>
			{
				{1, "One"},
				{2, "Two"},
				{3, "Three"},
				{4, "Four"},
			});
			_serializer.ShouldRoundtripEnumeration(new SortedDictionary<int, string>
			{
				{4, "Four"},
				{3, "Three"},
				{2, "Two"},
				{1, "One"},
			});
		}

		[Test]
		public void TestDictionary()
		{
			_serializer.ShouldRoundtripEnumeration(new Dictionary<int, string>());
			_serializer.ShouldRoundtripEnumeration(new Dictionary<int, string>
			{
				{5, "Who"},
				{4, "let"},
				{3, "the"},
				{2, "dogs"},
				{1, "out"},
			});
		}
	}
}