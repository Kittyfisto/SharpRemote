using System.IO;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Json
{
	[TestFixture]
	public sealed class JsonSerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		protected override ISerializer2 Create()
		{
			return new JsonSerializer();
		}

		protected override string Format(MemoryStream stream)
		{
			throw new System.NotImplementedException();
		}
	}
}