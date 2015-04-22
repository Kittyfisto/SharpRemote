using System.IO;
using System.Text;

namespace SharpRemote.CodeGeneration
{
	public static class SerializerExtensions
	{
		public static object RoundtripObject(this ISerializer serializer, object value)
		{
			using (var stream = new MemoryStream())
			{
				var writer = new BinaryWriter(stream, Encoding.UTF8);
				serializer.WriteObject(writer, value);
				writer.Flush();
				stream.Position = 0;

				var reader = new BinaryReader(stream, Encoding.UTF8);
				var actualValue = serializer.ReadObject(reader);
				return actualValue;
			}
		}
	}
}