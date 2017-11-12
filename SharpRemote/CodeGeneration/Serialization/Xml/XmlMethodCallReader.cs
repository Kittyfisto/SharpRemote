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
		public const string RpcElementName = XmlMethodCallWriter.RpcElementName;
		public const string RpcIdAttributeName = XmlMethodCallWriter.RpcIdAttributeName;
		public const string GrainIdAttributeName = XmlMethodCallWriter.GrainIdAttributeName;
		public const string MethodAttributeName = XmlMethodCallWriter.MethodAttributeName;
		public const string ArgumentElementName = XmlMethodCallWriter.ArgumentElementName;
		public const string ArgumentNameAttributeName = XmlMethodCallWriter.ArgumentNameAttributeName;
		public const string ArgumentValueAttributeName = XmlMethodCallWriter.ArgumentValueName;
		public const string ArgumentTypeAttributeName = XmlMethodCallWriter.ArgumentTypeAttributeName;

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
					case RpcIdAttributeName:
						id = ulong.Parse(_reader.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
						break;

					case GrainIdAttributeName:
						grainId = ulong.Parse(_reader.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
						break;

					case MethodAttributeName:
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

			if (_reader.Name != ArgumentElementName)
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
					case ArgumentTypeAttributeName:
						type = TypeResolver.GetType(_reader.Value, true);
						break;
				}
			}

			if (type == null)
				throw new NotImplementedException();

			_reader.Read();
			if (_reader.Name != ArgumentValueAttributeName)
				throw new NotImplementedException();

			var reader = _reader.ReadSubtree();
			var methods = _methodStorage.GetOrAdd(type);
			value = methods.ReadObjectDelegate(reader, _serializer, _endPoint);

			_reader.Read();
			return true;
		}

		public bool ReadNextArgumentAsSByte(out sbyte value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = sbyte.MinValue;
				return false;
			}
			value = sbyte.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsByte(out byte value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = byte.MinValue;
				return false;
			}
			value = byte.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsUInt16(out ushort value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = ushort.MinValue;
				return false;
			}
			value = ushort.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsInt16(out short value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = short.MinValue;
				return false;
			}
			value = short.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsUInt32(out uint value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = uint.MinValue;
				return false;
			}
			value = uint.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsInt32(out int value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = int.MinValue;
				return false;
			}
			value = int.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsUInt64(out ulong value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = ulong.MinValue;
				return false;
			}
			value = ulong.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsInt64(out long value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = long.MinValue;
				return false;
			}
			value = long.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsFloat(out float value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = float.MinValue;
				return false;
			}
			value = float.Parse(tmp, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsDouble(out double value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = double.MinValue;
				return false;
			}
			value = double.Parse(tmp, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsDecimal(out decimal value)
		{
			string tmp;
			if (!ReadNextArgument(out tmp))
			{
				value = decimal.MinValue;
				return false;
			}
			value = decimal.Parse(tmp, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsString(out string value)
		{
			return ReadNextArgument(out value);
		}

		public bool ReadNextArgumentAsBytes(out byte[] value)
		{
			string hexString;
			if (!ReadNextArgument(out hexString))
			{
				value = null;
				return false;
			}

			value = XmlSerializer.BytesFromHex(hexString);
			return true;
		}

		private bool ReadNextArgument()
		{
			if (!_reader.Read())
				return false;

			if (_reader.NodeType == XmlNodeType.EndElement && _reader.Name == RpcElementName)
				return false;

			return true;
		}

		private bool ReadNextArgument(out string value)
		{
			if (!ReadNextArgument())
			{
				value = null;
				return false;
			}

			value = null;
			var attributeCount = _reader.AttributeCount;
			for (var i = 0; i < attributeCount; ++i)
			{
				_reader.MoveToNextAttribute();

				switch (_reader.Name)
				{
					case ArgumentValueAttributeName:
						value = _reader.Value;
						break;
				}
			}

			return true;
		}
	}
}