using System.IO;
using System.Text;
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

		protected override void Save()
		{
			
		}

		protected override string Format(MemoryStream stream)
		{
			var value = stream.ToArray();
			var stringBuilder = new StringBuilder(value.Length * 2);
			foreach (var b in value)
				stringBuilder.AppendFormat("{0:x2}", b);
			return stringBuilder.ToString();
		}
	}
}