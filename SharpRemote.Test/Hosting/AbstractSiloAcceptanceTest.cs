using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public abstract class AbstractSiloAcceptanceTest
		: AbstractTest
	{
		private ISilo _silo;

		protected abstract ISilo Create();

		[TestFixtureSetUp]
		public new void TestFixtureSetUp()
		{
			_silo = Create();
		}

		[TestFixtureTearDown]
		public new void TestFixtureTearDown()
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
		public void TestCreate3()
		{
			var subject = _silo.CreateGrain<IGetStringProperty, GetStringPropertyImplementation>();
			subject.Value.Should().Be("Foobar");
		}

		[Test]
		[Description("Verifies that the create method is thread-safe")]
		public void TestCreate4()
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

		[Test]
		[Description("Verifies that CreateGrain throws when no default implementation has been registered first")]
		public void TestRegisterCreate4()
		{
			new Action(() => _silo.CreateGrain<IGetInt16Property>())
				.ShouldThrow<ArgumentException>()
				.WithMessage("There is no default implementation for interface type 'SharpRemote.Test.Types.Interfaces.PrimitiveTypes.IGetInt16Property' defined");
		}

		[Test]
		[Description("Verifies that after registering a default implementation, CreateGrain can be invoked without an implementation")]
		public void TestRegisterDefaultImplementation1()
		{
			_silo.RegisterDefaultImplementation<IGetInt32Property, Returns42>();
			var grain = _silo.CreateGrain<IGetInt32Property>();
			grain.Value.Should().Be(42);
		}

		[Test]
		[Description("Verifies that registering a default implementation for the same interface more than once is not allowed")]
		public void TestRegisterDefaultImplementation2()
		{
			_silo.RegisterDefaultImplementation<IGetInt16Property, Returns9000>();
			new Action(() => _silo.RegisterDefaultImplementation<IGetInt16Property, Returns9000>())
				.ShouldThrow<ArgumentException>()
				.WithMessage("There already is a default implementation for interface type 'SharpRemote.Test.Types.Interfaces.PrimitiveTypes.IGetInt16Property' defined");
		}

		[Test]
		[Description("Verifies that even when a default implementation is defined, it can be overwritten by specifying the implementation upon grain creation")]
		public void TestRegisterDefaultImplementation3()
		{
			_silo.RegisterDefaultImplementation<IGetInt64Property, ReturnsInt64Max>();
			_silo.CreateGrain<IGetInt64Property>().Value.Should().Be(long.MaxValue);
			_silo.CreateGrain<IGetInt64Property>(typeof (ReturnsNearlyInt64Max)).Value.Should().Be(long.MaxValue - 1);
		}
	}
}