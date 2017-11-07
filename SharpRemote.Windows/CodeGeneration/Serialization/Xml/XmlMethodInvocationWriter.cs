using System.IO;
using System.Xml;
using SharpRemote.Extensions;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodInvocationWriter
		: IMethodInvocationWriter
	{
		public const string RpcElementName = "RPC";
		public const string RpcIdElementName = "ID";
		public const string ArgumentElementName = "Argument";
		public const string ArgumentNameAttributeName = "Name";

		private readonly XmlSerializer _serializer;
		private readonly ulong _grainId;
		private readonly string _methodName;
		private readonly ulong _rpcId;
		private readonly StreamWriter _textWriter;
		private readonly XmlWriter _writer;
		private readonly IRemotingEndPoint _endPoint;

		public XmlMethodInvocationWriter(XmlSerializer serializer, XmlWriterSettings settings, Stream stream, ulong grainId, string methodName, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			_serializer = serializer;
			_grainId = grainId;
			_methodName = methodName;
			_rpcId = rpcId;
			_endPoint = endPoint;

			_textWriter = new StreamWriter(stream, settings.Encoding, 4096, true);
			_writer = XmlWriter.Create(_textWriter, settings);
			_writer.WriteStartDocument();
			_writer.WriteStartElement(RpcElementName);
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

		public void WriteNamedArgument(string name, sbyte value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteSByte(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, byte value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteByte(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, ushort value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteUInt16(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, short value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteInt16(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, uint value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteUInt32(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, int value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteInt32(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, ulong value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteUInt64(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, long value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteInt64(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, float value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteFloat(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, double value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteDouble(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteNamedArgument(string name, string value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			_writer.WriteAttributeString(ArgumentNameAttributeName, name);
			_serializer.WriteString(_writer, value);
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