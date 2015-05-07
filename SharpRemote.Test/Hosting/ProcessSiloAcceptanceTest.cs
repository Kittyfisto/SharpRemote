using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[Ignore]
	[TestFixture]
	public sealed class ProcessSiloAcceptanceTest
		: AbstractSiloAcceptanceTest
	{
		protected override ISilo Create()
		{
			return new ProcessSilo();
		}
	}
}