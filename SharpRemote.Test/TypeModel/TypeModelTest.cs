using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.TypeModel
{
	[TestFixture]
	public sealed class TypeModelTest
	{
		[Test]
		public void TestCtor()
		{
			var model = new SharpRemote.TypeModel();
			model.Types.Should().NotBeNull();
		}

		[Test]
		public void TestAddType1()
		{
			var model = new SharpRemote.TypeModel();
			model.Add(typeof(void));
			model.Types.Should().HaveCount(1);
			var type = model.Types.First();
			type.AssemblyQualifiedName.Should().Be(typeof(void).AssemblyQualifiedName);
		}
	}
}
