using System.Globalization;
using System.IO;
using System.Xml;
using SharpRemote.Extensions;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodInvocationWriter
		: IMethodInvocationWriter
	{
		public const string RpcElementName = "RPC";
		public const string RpcIdAttributeName = "ID";
		public const string GrainIdAttributeName = "Grain";
		public const string MethodAttributeName = "Method";
		public const string ArgumentElementName = "Argument";
		public const string ArgumentNameAttributeName = "Name";
		public const string ArgumentValueAttributeName = "Value";

		private readonly XmlSerializer _serializer;
		private readonly StreamWriter _textWriter;
		private readonly XmlWriter _writer;
		private readonly IRemotingEndPoint _endPoint;

		public XmlMethodInvocationWriter(XmlSerializer serializer, XmlWriterSettings settings, Stream stream, ulong grainId, string methodName, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			_serializer = serializer;
			_endPoint = endPoint;

			_textWriter = new StreamWriter(stream, settings.Encoding, 4096, true);
			_writer = XmlWriter.Create(_textWriter, settings);
			_writer.WriteStartDocument();

			_writer.WriteStartElement(RpcElementName);
			_writer.WriteAttributeString(RpcIdAttributeName, rpcId.ToString(CultureInfo.InvariantCulture));
			_writer.WriteAttributeString(GrainIdAttributeName, grainId.ToString(CultureInfo.InvariantCulture));
			_writer.WriteAttributeString(MethodAttributeName, methodName);
		}

		public void Dispose()
		{
			_writer.WriteEndElement();
			_writer.WriteEndDocument();
			_writer.TryDispose();
			_textWriter.TryDispose();
		}

		public void WriteNamedArgument(string name, object value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteObject(_writer, value, _endPoint);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument<T>(string name, T value) where T : struct
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteStruct(_writer, value, _endPoint);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, sbyte value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, byte value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, ushort value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, short value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, uint value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, int value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, ulong value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, long value)
		{
			WriteNamedArgument(name, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, float value)
		{
			WriteNamedArgument(name, value.ToString("R", CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, double value)
		{
			WriteNamedArgument(name, value.ToString("R", CultureInfo.InvariantCulture));
		}

		public void WriteNamedArgument(string name, string value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_writer.WriteAttributeString(ArgumentValueAttributeName, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, byte[] value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteBytes(_writer, value);
			_writer.WriteEndElement();
		}
	}
}