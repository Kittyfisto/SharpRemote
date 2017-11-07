using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	[TestFixture]
	public sealed class OutOfProcessSiloAcceptanceTest
		: AbstractSiloAcceptanceTest
	{
		protected override ISilo Create()
		{
			var silo = new OutOfProcessSilo();
			silo.Start();
			return silo;
		}
	}
}