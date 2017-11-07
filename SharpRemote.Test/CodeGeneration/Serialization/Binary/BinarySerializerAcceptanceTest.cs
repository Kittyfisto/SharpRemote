using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Binary
{
	[TestFixture]
	[Ignore("Not yet implemented")]
	public sealed class BinarySerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		protected override ISerializer2 Create()
		{
			return new BinarySerializer2();
		}
	}
}