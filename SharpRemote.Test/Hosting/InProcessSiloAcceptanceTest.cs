using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class InProcessSiloAcceptanceTest
		: AbstractSiloAcceptanceTest
	{
		protected override ISilo Create()
		{
			return new InProcessSilo();
		}
	}
}