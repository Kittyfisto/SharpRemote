using System;
using System.Globalization;
using System.IO;
using System.Xml;
using SharpRemote.Extensions;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodResultReader
		: IMethodResultReader
	{
		public const string RpcIdAttributeName = XmlMethodResultWriter.RpcIdAttributeName;

		private readonly XmlSerializer _serializer;
		private readonly StreamReader _textReader;
		private readonly XmlReader _reader;
		private readonly ulong _id;
		private readonly bool _isEmpty;

		public XmlMethodResultReader(XmlSerializer serializer, StreamReader textReader, XmlReader reader, SerializationMethodStorage<XmlMethodsCompiler> methodStorage, IRemotingEndPoint endPoint)
		{
			_serializer = serializer;
			_textReader = textReader;
			_reader = reader;

			ulong? id = null;
			var attributeCount = _reader.AttributeCount;
			for (int i = 0; i < attributeCount; ++i)
			{
				_reader.MoveToNextAttribute();
				switch (_reader.Name)
				{
					case RpcIdAttributeName:
						id = ulong.Parse(_reader.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
						break;
				}
			}

			// TODO: Throw appropriate exception with meaningful error message
			_id = id.Value;
			if (!_reader.Read())
				_isEmpty = true;
		}

		public void Dispose()
		{
			_reader.TryDispose();
			_textReader.TryDispose();
		}

		public ulong RpcId => _id;

		public bool ReadException(out Exception exception)
		{
			exception = null;
			if (_isEmpty)
				return false;

			var name = _reader.Name;
			switch (name)
			{
				case XmlSerializer.ReturnValueElementName:
					return false;

				case XmlSerializer.ExceptionElementName:
					// TODO: Read exception...
					return true;

				default:
					throw new XmlParseException(string.Format("Expected element '{0}' or '{1}', but found: {2}",
					                                          XmlSerializer.ReturnValueElementName,
					                                          XmlSerializer.ExceptionElementName,
					                                          name),
					                            ((IXmlLineInfo)_reader).LineNumber,
					                            ((IXmlLineInfo)_reader).LinePosition);
			}
		}

		public bool ReadResult(out object value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultSByte(out sbyte value)
		{
			value = sbyte.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsSByte(_reader);
			return true;
		}

		public bool ReadResultByte(out byte value)
		{
			value = byte.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsByte(_reader);
			return true;
		}

		public bool ReadResultUInt16(out ushort value)
		{
			value = ushort.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsUInt16(_reader);
			return true;
		}

		public bool ReadResultInt16(out short value)
		{
			value = short.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsInt16(_reader);
			return true;
		}

		public bool ReadResultUInt32(out uint value)
		{
			value = uint.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsUInt32(_reader);
			return true;
		}

		public bool ReadResultInt32(out int value)
		{
			value = int.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsInt32(_reader);
			return true;
		}

		public bool ReadResultUInt64(out ulong value)
		{
			value = ulong.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsUInt64(_reader);
			return true;
		}

		public bool ReadResultInt64(out long value)
		{
			value = long.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsInt64(_reader);
			return true;
		}

		public bool ReadResultFloat(out float value)
		{
			value = long.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsFloat(_reader);
			return true;
		}

		public bool ReadResultDouble(out double value)
		{
			value = long.MinValue;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsDouble(_reader);
			return true;
		}

		public bool ReadResultString(out string value)
		{
			value = null;
			if (!TryReadResult())
				return false;

			value = XmlSerializer.ReadValueAsString(_reader);
			return true;
		}

		public bool ReadResultBytes(out byte[] value)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Tries to read the result of the method call.
		/// </summary>
		/// <returns>true if a return value is present, false if its an exception</returns>
		/// <exception cref="XmlParseException">If the xml message is malformed</exception>
		private bool TryReadResult()
		{
			if (_isEmpty) //< True when the method didn't return a result
				return false;

			var name = _reader.Name;
			switch (name)
			{
				case XmlSerializer.ReturnValueElementName:
					return true;

				case XmlSerializer.ExceptionElementName:
					return false;

				default:
					throw new XmlParseException(string.Format("Expected element '{0}' or '{1}', but found: {2}",
					                                          XmlSerializer.ReturnValueElementName,
					                                          XmlSerializer.ExceptionElementName,
					                                          name),
					                            ((IXmlLineInfo)_reader).LineNumber,
					                            ((IXmlLineInfo)_reader).LinePosition);
			}
		}
	}
}