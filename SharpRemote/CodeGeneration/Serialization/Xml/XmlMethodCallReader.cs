using System;
using System.Globalization;
using System.IO;
using System.Xml;
using SharpRemote.Extensions;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodCallReader
		: IMethodCallReader
	{
		private readonly IRemotingEndPoint _endPoint;
		private readonly ulong _grainId;
		private readonly ulong _id;
		private readonly string _methodName;
		private readonly SerializationMethodStorage<XmlMethodsCompiler> _methodStorage;
		private readonly XmlReader _reader;

		private readonly XmlSerializer _serializer;
		private readonly StreamReader _textReader;

		public XmlMethodCallReader(XmlSerializer serializer,
		                                 StreamReader streamReader,
		                                 XmlReader reader,
		                                 SerializationMethodStorage<XmlMethodsCompiler> methodStorage,
		                                 IRemotingEndPoint endPoint)
		{
			_serializer = serializer;
			_endPoint = endPoint;
			_methodStorage = methodStorage;
			_textReader = streamReader;
			_reader = reader;

			ulong? id = null;
			ulong? grainId = null;
			var count = _reader.AttributeCount;
			for (var i = 0; i < count; ++i)
			{
				_reader.MoveToNextAttribute();
				switch (_reader.Name)
				{
					case XmlSerializer.RpcIdAttributeName:
						id = ulong.Parse(_reader.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
						break;

					case XmlSerializer.GrainIdAttributeName:
						grainId = ulong.Parse(_reader.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
						break;

					case XmlSerializer.MethodAttributeName:
						_methodName = _reader.Value;
						break;

					default:
						throw new NotImplementedException();
				}
			}

			// TODO: Throw exception with meaningful error message
			_id = id.Value;
			_grainId = grainId.Value;
		}

		public void Dispose()
		{
			_reader.TryDispose();
			_textReader.TryDispose();
		}

		public ulong RpcId => _id;

		public ulong GrainId => _grainId;

		public string MethodName => _methodName;

		public bool ReadNextArgument(out object value)
		{
			if (!ReadNextArgument())
			{
				value = null;
				return false;
			}

			if (_reader.Name != XmlSerializer.ArgumentElementName)
				throw new NotImplementedException();

			if (_reader.IsEmptyElement && !_reader.HasAttributes)
			{
				value = null;
				return true;
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

		public bool ReadNextArgumentAsSByte(out sbyte value)
		{
			if (!ReadNextArgument())
			{
				value = sbyte.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsSByte(_reader);
			return true;
		}

		public bool ReadNextArgumentAsByte(out byte value)
		{
			if (!ReadNextArgument())
			{
				value = byte.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsByte(_reader);
			return true;
		}

		public bool ReadNextArgumentAsUInt16(out ushort value)
		{
			if (!ReadNextArgument())
			{
				value = ushort.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsUInt16(_reader);
			return true;
		}

		public bool ReadNextArgumentAsInt16(out short value)
		{
			if (!ReadNextArgument())
			{
				value = short.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsInt16(_reader);
			return true;
		}

		public bool ReadNextArgumentAsUInt32(out uint value)
		{
			if (!ReadNextArgument())
			{
				value = uint.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsUInt32(_reader);
			return true;
		}

		public bool ReadNextArgumentAsInt32(out int value)
		{
			if (!ReadNextArgument())
			{
				value = int.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsInt32(_reader);
			return true;
		}

		public bool ReadNextArgumentAsUInt64(out ulong value)
		{
			if (!ReadNextArgument())
			{
				value = ulong.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsUInt64(_reader);
			return true;
		}

		public bool ReadNextArgumentAsInt64(out long value)
		{
			if (!ReadNextArgument())
			{
				value = long.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsInt64(_reader);
			return true;
		}

		public bool ReadNextArgumentAsSingle(out float value)
		{
			if (!ReadNextArgument())
			{
				value = float.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsSingle(_reader);
			return true;
		}

		public bool ReadNextArgumentAsDouble(out double value)
		{
			if (!ReadNextArgument())
			{
				value = double.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsDouble(_reader);
			return true;
		}

		public bool ReadNextArgumentAsDecimal(out decimal value)
		{
			if (!ReadNextArgument())
			{
				value = decimal.MinValue;
				return false;
			}
			value = XmlSerializer.ReadValueAsDecimal(_reader);
			return true;
		}

		public bool ReadNextArgumentAsString(out string value)
		{
			if (!ReadNextArgument())
			{
				value = null;
				return false;
			}

			value = XmlSerializer.ReadValueAsString(_reader);
			return true;
		}

		public bool ReadNextArgumentAsBytes(out byte[] value)
		{
			if (!ReadNextArgument())
			{
				value = null;
				return false;
			}

			value = XmlSerializer.ReadValueAsBytes(_reader);
			return true;
		}

		private bool ReadNextArgument()
		{
			if (!_reader.Read())
				return false;

			if (_reader.NodeType == XmlNodeType.EndElement && _reader.Name == XmlSerializer.MethodCallElementName)
				return false;

			return true;
		}
	}
}