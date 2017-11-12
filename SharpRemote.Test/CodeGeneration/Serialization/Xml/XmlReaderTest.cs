using System.IO;
using System.Text;
using System.Xml;
using NUnit.Framework;

namespace SharpRemote.Test.CodeGeneration.Serialization.Xml
{
	[TestFixture]
	public sealed class XmlReaderTest
	{
		[Test]
		public void TestRead()
		{
			using (var stream = new MemoryStream())
			{
				using (var textWriter = new StreamWriter(stream, Encoding.UTF8, 4096, true))
				using (var writer = XmlWriter.Create(textWriter, new XmlWriterSettings
				{
					Indent = true,
					NewLineHandling = NewLineHandling.Replace
				}))
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("Nodes");
					writer.WriteAttributeString("Version", "1");
					writer.WriteStartElement("Foo");
					writer.WriteStartElement("Child");
					writer.WriteEndElement();
					writer.WriteEndElement();
					writer.WriteStartElement("Bar");
					writer.WriteEndElement();
					writer.WriteEndElement();
					writer.WriteEndDocument();
				}
				Print(stream);
				stream.Position = 0;
				using (var textReader = new StreamReader(stream))
				using (var reader = XmlReader.Create(textReader, new XmlReaderSettings {IgnoreWhitespace = true}))
				{
					Read(reader);
					Read(reader);
					Read(reader);
					Read(reader);
					Read(reader);
					Read(reader);
					Read(reader);
					Read(reader);
				}
			}
		}

		public bool Stuff()
		{
			return true;
		}
		
		private void Print(MemoryStream stream)
		{
			WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
		}

		private static void MoveToContent(XmlReader reader)
		{
			reader.MoveToContent();
			WriteLine("MoveToContent() => {0}", Print(reader));
		}

		private static void MoveToElement(XmlReader reader)
		{
			reader.MoveToElement();
			WriteLine("MoveToElement() => {0}", Print(reader));
		}

		private static void Read(XmlReader reader)
		{
			reader.Read();
			WriteLine("Read() => {0}", Print(reader));
		}

		private static string Print(XmlReader reader)
		{
			var builder = new StringBuilder();
			builder.AppendFormat("Name: {0}, NodeType: {1}", reader.Name, reader.NodeType);
			if (reader.EOF)
				builder.Append(", EOF");
			return builder.ToString();
		}

		private static void WriteLine(string message, params object[] parameters)
		{
			TestContext.Out.WriteLine(message, parameters);
		}
	}
}