using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Xml
{
	[TestFixture]
	[Ignore("Not yet implemented")]
	public sealed class XmlSerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		protected override ISerializer2 Create()
		{
			return new XmlSerializer();
		}
	}
}