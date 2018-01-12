using System;
using System.IO;
using System.Text;
using System.Xml;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.CodeGeneration.Serialization.Xml;

namespace SharpRemote.Test.CodeGeneration.Serialization.Xml
{
	[TestFixture]
	public sealed class XmlFormatterTest
	{
		private Exception Roundtrip(Exception exception)
		{
			using (var stream = new MemoryStream())
			{
				var serializer = new XmlSerializer();
				
				using (var textWriter = new StreamWriter(stream, Encoding.UTF8, bufferSize: 4096, leaveOpen: true))
				using (var writer = XmlWriter.Create(textWriter, new XmlWriterSettings
				{
					Indent = true,
					NewLineHandling = NewLineHandling.Replace
				}))
				{
					writer.WriteStartDocument();
					writer.WriteStartElement("Exception");
					XmlFormatter.Write(writer, serializer, exception);
					writer.WriteEndElement();
					writer.WriteEndDocument();
				}

				TestContext.Out.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));

				stream.Position = 0;

				using (var reader = XmlReader.Create(stream, new XmlReaderSettings
				{
					IgnoreWhitespace = true
				}))
				{
					reader.MoveToContent();
					var actualException = XmlFormatter.Read(reader, serializer, new TypeResolver());
					actualException.Should().NotBeNull();
					actualException.Should().NotBeSameAs(exception);
					return actualException;
				}
			}
		}

		[Test]
		[Description("Verifies that an Exception can be roundtripped")]
		public void TestRoundtripException1()
		{
			var actual = Roundtrip(new Exception("Hello, World!"));
			actual.Message.Should().Be("Hello, World!");
		}
	}
}