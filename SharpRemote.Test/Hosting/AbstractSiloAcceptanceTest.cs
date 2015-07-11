using System;
using System.Linq;
using System.Threading.Tasks;
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

		[Test]
		public void TestCreate2()
		{
			var subject = _silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation).AssemblyQualifiedName);
			subject.Value.Should().Be("Foobar");
		}

		[Test]
		[Ignore]
		[Description("Verifies that the create method is thread-safe")]
		public void TestCreate3()
		{
			Func<IGetStringProperty> fn = () => _silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
			var tasks = Enumerable.Range(0, 50).Select(i => Task<IGetStringProperty>.Factory.StartNew(fn)).ToArray();
			Task.WaitAll(tasks, TimeSpan.FromSeconds(10)).Should().BeTrue("Because that should be plenty of time");

			var objects = tasks.Select(x => x.Result).ToList();
			objects.TrueForAll(x => x.Value == "Foobar").Should().BeTrue();
		}
	}
}