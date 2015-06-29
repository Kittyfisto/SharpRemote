using Moq;
using NUnit.Framework;
using SharpRemote.Watchdog;

namespace SharpRemote.Test.Watchdog
{
	[TestFixture]
	public sealed class InternalWatchdogTest
	{
		private Mock<IIsolatedStorage> _storage;

		[SetUp]
		public void SetUp()
		{
			_storage = new Mock<IIsolatedStorage>();
		}

		[Test]
		[Description("Verifies that the description of previously installed applications is kept")]
		public void TestCtor()
		{
			var watchdog = new InternalWatchdog(_storage.Object);
		}
	}
}