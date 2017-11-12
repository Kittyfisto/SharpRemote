using System;
using System.Globalization;
using System.IO;
using System.Xml;
using SharpRemote.Extensions;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodResultWriter
		: IMethodResultWriter
	{
		public const string RpcElementName = "Result";
		public const string RpcIdAttributeName = XmlMethodCallWriter.RpcIdAttributeName;
		public const string ResultElementName = "ReturnValue";
		public const string ExceptionElementName = "Exception";

		private readonly XmlSerializer _serializer;
		private readonly StreamWriter _textWriter;
		private readonly XmlWriter _writer;
		private readonly IRemotingEndPoint _endPoint;

		public XmlMethodResultWriter(XmlSerializer serializer, XmlWriterSettings settings, Stream stream, ulong rpcId, IRemotingEndPoint endPoint = null)
		{
			_serializer = serializer;
			_endPoint = endPoint;
			_textWriter = new StreamWriter(stream, settings.Encoding, 4096, leaveOpen: true);
			_writer = XmlWriter.Create(_textWriter, settings);
			_writer.WriteStartDocument();
			_writer.WriteStartElement(RpcElementName);
			_writer.WriteAttributeString(RpcIdAttributeName, rpcId.ToString(CultureInfo.InvariantCulture));
		}

		public void Dispose()
		{
			_writer.WriteEndElement();
			_writer.WriteEndDocument();
			_writer.TryDispose();
			_textWriter.TryDispose();
		}

		public void WriteFinished()
		{
			_writer.WriteStartElement(ResultElementName);
			_writer.WriteEndElement();
		}

		public void WriteResult(object value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteObject(_writer, value, _endPoint);
			_writer.WriteEndElement();
		}

		public void WriteResult(sbyte value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteSByte(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(byte value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteByte(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(ushort value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteUInt16(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(short value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteInt16(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(uint value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteUInt32(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(int value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteInt32(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(ulong value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteUInt64(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(long value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteInt64(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(float value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteFloat(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(double value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteDouble(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(string value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteString(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteResult(byte[] value)
		{
			_writer.WriteStartElement(ResultElementName);
			_serializer.WriteBytes(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteException(Exception e)
		{
			_writer.WriteStartElement(ExceptionElementName);
			_serializer.WriteObject(_writer, e, _endPoint);
			_writer.WriteEndElement();
		}
	}
}