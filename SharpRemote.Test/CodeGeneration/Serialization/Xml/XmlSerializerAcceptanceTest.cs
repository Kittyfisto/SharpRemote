using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Xml
{
	[TestFixture]
	public sealed class XmlSerializerAcceptanceTest
		: AbstractSerializerAcceptanceTest
	{
		protected override ISerializer2 Create()
		{
			return new XmlSerializer();
		}

		protected override string Format(MemoryStream stream)
		{
			using (var reader = new StreamReader(stream, Encoding.Default, true, 4096, true))
			{
				return reader.ReadToEnd();
			}
		}

		public static IEnumerable<ulong> GrainIds => new ulong[]
		{
			ulong.MinValue,
			1,
			uint.MaxValue,
			ulong.MaxValue
		};

		public static IEnumerable<ulong> RpcIds => new ulong[]
		{
			ulong.MinValue,
			42,
			1337,
			ulong.MaxValue
		};

	}
}