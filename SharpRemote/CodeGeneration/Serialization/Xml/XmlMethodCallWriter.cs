﻿using System.Globalization;
using System.IO;
using System.Xml;
using SharpRemote.Extensions;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodCallWriter
		: IMethodCallWriter
	{
		public const string RpcElementName = "Call";
		public const string RpcIdAttributeName = "ID";
		public const string GrainIdAttributeName = "Grain";
		public const string MethodAttributeName = "Method";
		public const string ArgumentElementName = "Argument";
		public const string ArgumentNameAttributeName = "Name";
		public const string ArgumentValueName = "Value";
		public const string ArgumentTypeAttributeName = "Type";

		private readonly XmlSerializer _serializer;
		private readonly StreamWriter _textWriter;
		private readonly XmlWriter _writer;
		private readonly IRemotingEndPoint _endPoint;

		public XmlMethodCallWriter(XmlSerializer serializer, XmlWriterSettings settings, Stream stream, ulong grainId, string methodName, ulong rpcId, IRemotingEndPoint endPoint = null)
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

		public void WriteArgument(object value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			if (value != null)
			{
				_writer.WriteAttributeString(ArgumentTypeAttributeName, value.GetType().AssemblyQualifiedName);
				_writer.WriteStartElement(ArgumentValueName);
				_serializer.WriteObject(_writer, value, _endPoint);
				_writer.WriteEndElement();
			}
			_writer.WriteEndElement();
		}

		public void WriteArgument(sbyte value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(byte value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(ushort value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(short value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(uint value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(int value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(ulong value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(long value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(float value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(double value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(decimal value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(string value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			if (value != null)
				XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}

		public void WriteArgument(byte[] value)
		{
			_writer.WriteStartElement(ArgumentElementName);
			XmlSerializer.WriteValue(_writer, value);
			_writer.WriteEndElement();
		}
	}
}