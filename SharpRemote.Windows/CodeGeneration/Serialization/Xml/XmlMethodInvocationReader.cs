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

		private readonly XmlSerializer _xmlSerializer;
		private readonly StreamReader _textReader;
		private readonly XmlReader _reader;
		private readonly ulong _id;
		private readonly ulong _grainId;
		private readonly string _methodName;

		public XmlMethodInvocationReader(XmlSerializer xmlSerializer, Stream stream)
		{
			_xmlSerializer = xmlSerializer;
			_textReader = new StreamReader(stream, Encoding.Default, true, 4096, true);
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

		public object ReadNextArgument(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public sbyte ReadNextArgumentAsSByte(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public byte ReadNextArgumentAsByte(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public ushort ReadNextArgumentAsUInt16(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public short ReadNextArgumentAsInt16(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public uint ReadNextArgumentAsUInt32(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public int ReadNextArgumentAsInt32(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public ulong ReadNextArgumentAsUInt64(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public long ReadNextArgumentAsInt64(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public float ReadNextArgumentAsFloat(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public double ReadNextArgumentAsDouble(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public string ReadNextArgumentAsString(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public byte[] ReadNextArgumentAsBytes(out string argumentName)
		{
			throw new NotImplementedException();
		}
	}
}