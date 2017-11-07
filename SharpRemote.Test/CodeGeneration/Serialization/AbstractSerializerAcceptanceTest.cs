using System.IO;
using FluentAssertions;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization
{
	[TestFixture]
	public abstract class AbstractSerializerAcceptanceTest
	{
		protected abstract ISerializer2 Create();

		[Test]
		public void TestEmptyMethodCall()
		{
			var serializer = Create();
			using (var stream = new MemoryStream())
			{
				using (serializer.CreateMethodInvocationWriter(stream, grainId: 42, methodName: "Foo", rpcId: 1337))
				{
				}

				stream.Position.Should()
				      .BeGreaterThan(expected: 0, because: "because some data must've been written to the stream");
				stream.Position = 0;
				using (var reader = serializer.CreateMethodInvocationReader(stream))
				{
					reader.GrainId.Should().Be(expected: 42);
					reader.MethodName.Should().Be("Foo");
					reader.RpcId.Should().Be(expected: 1337);
				}
			}
		}
	}
}