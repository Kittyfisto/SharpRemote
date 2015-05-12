using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Policy;
using NUnit.Framework;
using SharpRemote.CodeGeneration.Serialization;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public partial class SerializationTest
	{
		[Test]
		public void TestKeyValuePair()
		{
			_serializer.ShouldRoundtrip(new KeyValuePair<int, string>(42, "FOobar"));
			_serializer.ShouldRoundtrip(new KeyValuePair<int, KeyValuePair<string, object>>(42, new KeyValuePair<string, object>("Foobar", typeof(int))));
		}

		[Test]
		public void TestType()
		{
			_serializer.RegisterType<Type>();
			_serializer.ShouldRoundtrip(typeof(int));
		}

		[Test]
		public void TestIPAddress()
		{
			_serializer.RegisterType<IPAddress>();
			_serializer.ShouldRoundtrip(IPAddress.Parse("192.168.0.87"));
			_serializer.ShouldRoundtrip(IPAddress.IPv6Loopback);
		}

		[Test]
		public void TestIPEndPoint()
		{
			var ep = new IPEndPoint(IPAddress.Parse("192.168.0.87"), 80);
			_serializer.ShouldRoundtrip(ep);

			ep = new IPEndPoint(IPAddress.IPv6Loopback, 55980);
			_serializer.ShouldRoundtrip(ep);
		}

		[Test]
		public void TestTimeSpan()
		{
			_serializer.ShouldRoundtrip(TimeSpan.Zero);
			_serializer.ShouldRoundtrip(TimeSpan.FromSeconds(1.5));
			_serializer.ShouldRoundtrip(TimeSpan.FromDays(4));
			_serializer.ShouldRoundtrip(TimeSpan.FromDays(-4));
			_serializer.ShouldRoundtrip(TimeSpan.MinValue);
			_serializer.ShouldRoundtrip(TimeSpan.MaxValue);
		}

		[Test]
		public void TestDateTime()
		{
			_serializer.ShouldRoundtrip(DateTime.Now);
			_serializer.ShouldRoundtrip(DateTime.UtcNow);
			_serializer.ShouldRoundtrip(new DateTime(2015, 5, 12, 20, 00, 23, DateTimeKind.Local));
			_serializer.ShouldRoundtrip(new DateTime(2015, 5, 12, 20, 00, 23, DateTimeKind.Unspecified));
			_serializer.ShouldRoundtrip(new DateTime(2015, 5, 12, 20, 00, 23, DateTimeKind.Utc));
			_serializer.ShouldRoundtrip(DateTime.MinValue);
			_serializer.ShouldRoundtrip(DateTime.MaxValue);
		}

		[Test]
		public void TestVersion()
		{
			_serializer.ShouldRoundtrip(new Version(4, 0, 3211, 45063));
			_serializer.ShouldRoundtrip(new Version(0, 0, 0, 0));
			_serializer.ShouldRoundtrip(new Version(int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue));
		}

		[Test]
		public void TestApplicationId()
		{
			var appId = new ApplicationId(new byte[512],
			                              "SharpRemote",
			                              new Version(1, 2, 3, 4),
			                              "x86",
			                              "de-de");
			_serializer.ShouldRoundtrip(appId);
		}

		[Test]
		public void TestList()
		{
			_serializer.ShouldRoundtripEnumeration(new List<int> { 1, 2, 3, 4 });
			_serializer.ShouldRoundtripEnumeration(new List<int> { 1 });
			_serializer.ShouldRoundtripEnumeration(new List<int>());
			_serializer.ShouldRoundtripEnumeration(new List<int> { 9001, int.MinValue, int.MaxValue });
		}

		[Test]
		public void TestHashSet()
		{
			_serializer.ShouldRoundtripEnumeration(new HashSet<int>());
			_serializer.ShouldRoundtripEnumeration(new HashSet<int> { 1, 2, 3, 4 });

			var value = new HashSet<string>(
				Enumerable.Range(0, 10000)
				.Select(x => x.ToString(CultureInfo.InvariantCulture))
				);
			_serializer.ShouldRoundtripEnumeration(value);

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
		public void TestQueue()
		{
			_serializer.ShouldRoundtripEnumeration(new Queue<int>());

			var values = new Queue<int>();
			values.Enqueue(1);
			values.Enqueue(5);
			values.Enqueue(4);
			values.Enqueue(1);
			values.Enqueue(10);

			_serializer.ShouldRoundtripEnumeration(values);
		}

		[Test]
		public void TestSortedSet()
		{
			_serializer.ShouldRoundtripEnumeration(new SortedSet<string>());
			_serializer.ShouldRoundtripEnumeration(new SortedSet<string>
			{
				"a", "b", "", "foobar", "wookie"
			});
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