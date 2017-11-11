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
		}

		public void Dispose()
		{
			_reader.TryDispose();
			_textReader.TryDispose();
		}

		public ulong RpcId => _id;

		public object ReadResult(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public sbyte ReadResultAsSByte(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public byte ReadResultAsByte(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public ushort ReadResultAsUInt16(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public short ReadResultAsInt16(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public uint ReadResultUInt32(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public int ReadResultAsInt32(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public ulong ReadResultAsUInt64(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public long ReadResultAsInt64(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public float ReadResultAsFloat(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public double ReadResultAsDouble(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public string ReadResultAsString(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public byte[] ReadResultAsBytes(out Exception exception)
		{
			throw new NotImplementedException();
		}
	}
}