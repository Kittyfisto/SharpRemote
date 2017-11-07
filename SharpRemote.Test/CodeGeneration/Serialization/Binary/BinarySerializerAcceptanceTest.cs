using System.IO;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Binary
{
	[TestFixture]
	public sealed class BinarySerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		protected override ISerializer2 Create()
		{
			return new BinarySerializer2();
		}

		protected override string Format(MemoryStream stream)
		{
			throw new System.NotImplementedException();
		}
	}
}