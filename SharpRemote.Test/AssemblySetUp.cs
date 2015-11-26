using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace SharpRemote.Test
{
	[SetUpFixture]
	public sealed class AssemblySetUp
	{
		[SetUp]
		public void RunBeforeAnyTests()
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		}
	}
}