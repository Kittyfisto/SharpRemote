using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class ProcessSiloTest
	{
		[Test]
		public void TestCreateGrain1()
		{
			using (var silo = new ProcessSiloClient())
			{
				var proxy = silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
				proxy.Value.Should().Be("Foobar");
			}
		}

		[Test]
		public void TestCtor()
		{
			using (var silo = new ProcessSiloClient())
			{
				silo.IsProcessRunning.Should().BeTrue();
			}
		}
	}
}