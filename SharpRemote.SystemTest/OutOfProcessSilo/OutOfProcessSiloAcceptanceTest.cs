using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.Hosting;

namespace SharpRemote.SystemTest.OutOfProcessSilo
{
	[TestFixture]
	public sealed class OutOfProcessSiloAcceptanceTest
		: AbstractSiloAcceptanceTest
	{
		protected override ISilo Create()
		{
			var silo = new SharpRemote.Hosting.OutOfProcessSilo();
			silo.Start();
			return silo;
		}
	}
}