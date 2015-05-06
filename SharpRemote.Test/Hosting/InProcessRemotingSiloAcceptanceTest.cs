using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class InProcessRemotingSiloAcceptanceTest
		: AbstractSiloAcceptanceTest
	{
		protected override ISilo Create()
		{
			return new InProcessRemotingSilo();
		}
	}
}