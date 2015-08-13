using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Extensions;

namespace SharpRemote.Test.Extensions
{
	[TestFixture]
	public sealed class ProcessExtensionsTest
	{
		[Test]
		[Description("Verifies that TryKill actually kills the process")]
		public void TestTryKill1()
		{
			var process = new Process {StartInfo = new ProcessStartInfo("SharpRemote.Host.exe")};
			try
			{
				process.Start();

				process.TryKill().Should().BeTrue();
				process.HasExited.Should().BeTrue();
			}
			finally
			{
				try
				{
					process.Kill(); //< We don'T want no dangling process when this test fails
				}
				catch
				{
					
				}
			}
		}
	}
}