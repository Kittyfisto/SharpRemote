using System;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class ProcessWatchdogTest
	{
		[Test]
		public void TestDispose()
		{
			var watchdog = new ProcessWatchdog();
			new Action(watchdog.Dispose).ShouldNotThrow();
		}
	}
}