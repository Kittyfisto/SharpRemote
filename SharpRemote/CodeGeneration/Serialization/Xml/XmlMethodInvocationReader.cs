using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using SharpRemote.Extensions;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodInvocationReader
		: IMethodInvocationReader
	{
		public const string RpcElementName = XmlMethodInvocationWriter.RpcElementName;
		public const string RpcIdAttributeName = XmlMethodInvocationWriter.RpcIdAttributeName;
		public const string GrainIdAttributeName = XmlMethodInvocationWriter.GrainIdAttributeName;
		public const string MethodAttributeName = XmlMethodInvocationWriter.MethodAttributeName;
		public const string ArgumentNameAttributeName = XmlMethodInvocationWriter.ArgumentNameAttributeName;
		public const string ArgumentValueAttributeName = XmlMethodInvocationWriter.ArgumentValueAttributeName;

		private readonly XmlSerializer _xmlSerializer;
		private readonly StreamReader _textReader;
		private readonly XmlReader _reader;
		private readonly ulong _id;
		private readonly ulong _grainId;
		private readonly string _methodName;

		public XmlMethodInvocationReader(XmlSerializer xmlSerializer, Encoding encoding, Stream stream)
		{
			_xmlSerializer = xmlSerializer;
			_textReader = new StreamReader(stream, encoding, true, 4096, true);
			_reader = XmlReader.Create(_textReader);
			_reader.MoveToContent();

			ulong? id = null;
			ulong? grainId = null;
			int count = _reader.AttributeCount;
			for (int i = 0; i < count; ++i)
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

		public bool ReadNextArgument(out string name, out object value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsStruct<T>(out string name, out T value) where T : struct
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsSByte(out string name, out sbyte value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = sbyte.MinValue;
				return false;
			}
			value = sbyte.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsByte(out string name, out byte value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = byte.MinValue;
				return false;
			}
			value = byte.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsUInt16(out string name, out ushort value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = ushort.MinValue;
				return false;
			}
			value = ushort.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsInt16(out string name, out short value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = short.MinValue;
				return false;
			}
			value = short.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsUInt32(out string name, out uint value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = uint.MinValue;
				return false;
			}
			value = uint.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsInt32(out string name, out int value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = int.MinValue;
				return false;
			}
			value = int.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsUInt64(out string name, out ulong value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = ulong.MinValue;
				return false;
			}
			value = ulong.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsInt64(out string name, out long value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = long.MinValue;
				return false;
			}
			value = long.Parse(tmp, NumberStyles.Integer, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsFloat(out string name, out float value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = float.MinValue;
				return false;
			}
			value = float.Parse(tmp, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsDouble(out string name, out double value)
		{
			string tmp;
			if (!ReadNextArgument(out name, out tmp))
			{
				value = double.MinValue;
				return false;
			}
			value = double.Parse(tmp, CultureInfo.InvariantCulture);
			return true;
		}

		public bool ReadNextArgumentAsString(out string name, out string value)
		{
			return ReadNextArgument(out name, out value);
		}

		public bool ReadNextArgumentAsBytes(out string name, out byte[] value)
		{
			throw new NotImplementedException();
		}

		private bool ReadNextArgument()
		{
			if (!_reader.Read())
				return false;

			if (_reader.NodeType == XmlNodeType.EndElement && _reader.Name == RpcElementName)
				return false;

			return true;
		}

		private bool ReadNextArgument(out string name, out string value)
		{
			if (!ReadNextArgument())
			{
				name = null;
				value = null;
				return false;
			}

			name = null;
			value = null;
			var attributeCount = _reader.AttributeCount;
			for (int i = 0; i < attributeCount; ++i)
			{
				_reader.MoveToNextAttribute();

				switch (_reader.Name)
				{
					case ArgumentNameAttributeName:
						name = _reader.Value;
						break;

					case ArgumentValueAttributeName:
						value = _reader.Value;
						break;
				}
			}

			// TODO: Throw better exception
			if (name == null)
				throw new NotImplementedException();
			if (value == null)
				throw new NotImplementedException();
			return true;
		}
	}
}