using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Hosting
{
	[Ignore]
	[TestFixture]
	public sealed class ProcessSiloTest
	{
		[Test]
		public void TestCtor()
		{
			using (var silo = new ProcessSilo())
			{
				silo.CreateGrain<IGetStringProperty>(typeof (GetStringPropertyImplementation));
			}
		}
	}
}