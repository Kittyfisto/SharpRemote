using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public abstract class AbstractSiloAcceptanceTest
	{
		private ISilo _silo;

		protected abstract ISilo Create();

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			_silo = Create();
		}

		[TestFixtureTearDown]
		public void TestFixtureTearDown()
		{
			_silo.Dispose();
			_silo = null;
		}

		[Test]
		public void TestCreate1()
		{
			var subject = _silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
			subject.Value.Should().Be("Foobar");
		}
	}
}