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

		[Test]
		[Description("Verifies that RegisterDefaultImplementation can retrieve the proper type from its assembly qualified name")]
		public void TestRegisterDefaultImplementation1()
		{
			using (var silo = new InProcessSilo())
			{
				silo.RegisterDefaultImplementation<IVoidMethodNoParameters>(typeof(AbortsThread).AssemblyQualifiedName);
				var grain = silo.CreateGrain<IVoidMethodNoParameters>();
				grain.Should().BeOfType<AbortsThread>();
			}
		}

		[Test]
		[Description("Verifies that RegisterDefaultImplementation uses the custom type resolver we've specified in the ctor")]
		public void TestRegisterDefaultImplementation2()
		{
			var customTypeResolver = new CustomTypeResolver1();
			using (var silo = new InProcessSilo(customTypeResolver))
			{
				customTypeResolver.GetTypeCalled.Should().Be(0);
				silo.RegisterDefaultImplementation<IVoidMethodNoParameters>(typeof(AbortsThread).AssemblyQualifiedName);
				customTypeResolver.GetTypeCalled.Should().Be(1, "because the silo should've used the custom type resolver we've specified in the ctor");

				var grain = silo.CreateGrain<IVoidMethodNoParameters>();
				grain.Should().BeOfType<AbortsThread>();
			}
		}
	}
}