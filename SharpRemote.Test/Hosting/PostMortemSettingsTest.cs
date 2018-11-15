using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class PostMortemSettingsTest
	{
		[Test]
		public void TestClone([Values(true, false)] bool suppressErrorWindows,
			[Values(true, false)] bool collectMinidumps,
			[Values(true, false)] bool handleAccessViolations,
			[Values(true, false)] bool handleCrtAsserts,
			[Values(true, false)] bool handleCrtPureVirtualFunctionCalls,
			[Values(0, 1)] int numMinidumpsRetained
			)
		{
			var config = new PostMortemSettings
			{
				SuppressErrorWindows = suppressErrorWindows,
				CollectMinidumps = collectMinidumps,
				HandleAccessViolations = handleAccessViolations,
				HandleCrtAsserts = handleCrtAsserts,
				HandleCrtPureVirtualFunctionCalls = handleCrtPureVirtualFunctionCalls,
				NumMinidumpsRetained = numMinidumpsRetained,
				MinidumpFolder = "a",
				MinidumpName = "b",
				LogFileName = @"C:\foo.log",
				RuntimeVersions = CRuntimeVersions._100 | CRuntimeVersions.Release
			};

			var actualConfig = config.Clone();
			actualConfig.SuppressErrorWindows.Should().Be(suppressErrorWindows);
			actualConfig.CollectMinidumps.Should().Be(collectMinidumps);
			actualConfig.HandleAccessViolations.Should().Be(handleAccessViolations);
			actualConfig.HandleCrtAsserts.Should().Be(handleCrtAsserts);
			actualConfig.HandleCrtPureVirtualFunctionCalls.Should().Be(handleCrtPureVirtualFunctionCalls);
			actualConfig.NumMinidumpsRetained.Should().Be(numMinidumpsRetained);
			actualConfig.MinidumpFolder.Should().Be("a");
			actualConfig.MinidumpName.Should().Be("b");
			actualConfig.LogFileName.Should().Be(@"C:\foo.log");
			actualConfig.RuntimeVersions.Should().Be(CRuntimeVersions._100 | CRuntimeVersions.Release);
		}
	}
}
