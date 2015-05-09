using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
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