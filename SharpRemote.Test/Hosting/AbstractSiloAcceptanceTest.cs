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
		[Description("Verifies that the create method is thread-safe")]
		public void TestCreate3()
		{
			const int numTries = 1000;
			Action fn = () =>
				{
					for (int i = 0; i < numTries; ++i)
					{
						var proxy = _silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
						proxy.Value.Should().Be("Foobar");
					}
				};

			var tasks = new[]
				{
					Task.Factory.StartNew(fn),
					Task.Factory.StartNew(fn)
				};
			Task.WaitAll(tasks, TimeSpan.FromSeconds(10)).Should().BeTrue();
			tasks.All(x => x.IsFaulted).Should().BeFalse();
		}
	}
}