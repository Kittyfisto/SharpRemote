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
		private readonly XmlSerializer _serializer;
		private readonly StreamReader _textReader;
		private readonly XmlReader _reader;
		private readonly SerializationMethodStorage<XmlMethodsCompiler> _methodStorage;
		private readonly IRemotingEndPoint _endPoint;
		private readonly ulong _id;
		private readonly bool _isEmpty;

		public XmlMethodResultReader(XmlSerializer serializer,
		                             StreamReader textReader,
		                             XmlReader reader,
		                             SerializationMethodStorage<XmlMethodsCompiler> methodStorage,
		                             IRemotingEndPoint endPoint)
		{
			_serializer = serializer;
			_textReader = textReader;
			_reader = reader;
			_methodStorage = methodStorage;
			_endPoint = endPoint;

			ulong? id = null;
			var attributeCount = _reader.AttributeCount;
			for (int i = 0; i < attributeCount; ++i)
			{
				_reader.MoveToNextAttribute();
				switch (_reader.Name)
				{
					case XmlSerializer.RpcIdAttributeName:
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
			if (!TryReadResult())
			{
				value = null;
				return false;
			}

			var count = _reader.AttributeCount;
			Type type = null;
			for (int i = 0; i < count; ++i)
			{
				_reader.MoveToAttribute(i);
				switch (_reader.Name)
				{
					case XmlSerializer.TypeAttributeName:
						type = TypeResolver.GetType(_reader.Value, true);
						break;
				}
			}

			if (type == null)
				throw new NotImplementedException();

			_reader.Read();
			if (_reader.Name != XmlSerializer.ValueName)
				throw new NotImplementedException();

			var methods = _methodStorage.GetOrAdd(type);
			value = methods.ReadObjectDelegate(_reader, _serializer, _endPoint);

			_reader.MoveToElement();
			_reader.Read();
			return true;
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
			if (!TryReadResult())
			{
				value = ushort.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsUInt16(_reader);
			return true;
		}

		public bool ReadResultInt16(out short value)
		{
			if (!TryReadResult())
			{
				value = short.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsInt16(_reader);
			return true;
		}

		public bool ReadResultUInt32(out uint value)
		{
			if (!TryReadResult())
			{
				value = uint.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsUInt32(_reader);
			return true;
		}

		public bool ReadResultInt32(out int value)
		{
			if (!TryReadResult())
			{
				value = int.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsInt32(_reader);
			return true;
		}

		public bool ReadResultUInt64(out ulong value)
		{
			if (!TryReadResult())
			{
				value = ulong.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsUInt64(_reader);
			return true;
		}

		public bool ReadResultInt64(out long value)
		{
			if (!TryReadResult())
			{
				value = long.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsInt64(_reader);
			return true;
		}

		public bool ReadResultSingle(out float value)
		{
			if (!TryReadResult())
			{
				value = long.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsSingle(_reader);
			return true;
		}

		public bool ReadResultDouble(out double value)
		{
			if (!TryReadResult())
			{
				value = long.MinValue;
				return false;
			}

			value = XmlSerializer.ReadValueAsDouble(_reader);
			return true;
		}

		public bool ReadResultString(out string value)
		{
			if (!TryReadResult())
			{
				value = null;
				return false;
			}

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