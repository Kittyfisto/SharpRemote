using System.Collections.Generic;
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
			_serializer.ShouldRoundtripEnumeration(new HashSet<int> {1, 2, 3, 4});
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