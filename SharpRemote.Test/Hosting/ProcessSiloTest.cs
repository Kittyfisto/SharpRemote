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
		public void TestCtor()
		{
			using (var silo = new ProcessSilo())
			{
				silo.IsProcessRunning.Should().BeTrue();
			}
		}

		[Test]
		public void TestCreateGrain()
		{
			using (var silo = new ProcessSilo())
			{
				silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
			}
		}
	}
}