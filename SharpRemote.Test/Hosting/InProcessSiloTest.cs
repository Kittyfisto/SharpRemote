using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Hosting;
using SharpRemote.Test.CodeGeneration.Serialization;
using SharpRemote.Test.Types.Classes;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Hosting
{
	[TestFixture]
	public sealed class InProcessSiloTest
	{
		[Test]
		[Description("Verifies that the create method uses the custom type resolver, if specified, to resolve types")]
		public void TestCreate()
		{
			var customTypeResolver = new CustomTypeResolver1();
			using (var silo = new InProcessSilo(customTypeResolver))
			{
				customTypeResolver.GetTypeCalled.Should().Be(0);
				var grain = silo.CreateGrain<IVoidMethodNoParameters>(typeof(AbortsThread).AssemblyQualifiedName);
				customTypeResolver.GetTypeCalled.Should().Be(1, "because the silo should've used the custom type resolver we've specified in the ctor");
			}
		}
	}
}