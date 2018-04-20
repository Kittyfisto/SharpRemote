using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodCallReader
		: IMethodCallReader
	{
		private readonly Stream _stream;
		private readonly BinarySerializer2 _serializer;
		private readonly BinaryReader _reader;
		private readonly ulong _grainId;
		private readonly string _methodName;
		private readonly ulong _rpcId;

		public BinaryMethodCallReader(BinarySerializer2 serializer, BinaryReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			_serializer = serializer;
			_reader = reader;
			_stream = reader.BaseStream;
			_grainId = _reader.ReadUInt64();
			_methodName = _reader.ReadString();
			_rpcId = _reader.ReadUInt64();
		}

		private bool EndOfStream => _stream.Position >= _stream.Length;

		public void Dispose()
		{
			_reader.Dispose();
		}

		public ulong GrainId => _grainId;

		public string MethodName => _methodName;

		public ulong RpcId => _rpcId;

		public bool ReadNextArgument(out object value)
		{
			if (EndOfStream)
			{
				value = null;
				return false;
			}

			value = _serializer.ReadObject(_reader);
			return true;
		}

		public bool ReadNextArgumentAsStruct<T>(out T value) where T : struct
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsSByte(out sbyte value)
		{
			if (EndOfStream)
			{
				value = sbyte.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsSByte(_reader);
			return true;
		}

		public bool ReadNextArgumentAsByte(out byte value)
		{
			if (EndOfStream)
			{
				value = byte.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsByte(_reader);
			return true;
		}

		public bool ReadNextArgumentAsUInt16(out ushort value)
		{
			if (EndOfStream)
			{
				value = ushort.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsUInt16(_reader);
			return true;
		}

		public bool ReadNextArgumentAsInt16(out short value)
		{
			if (EndOfStream)
			{
				value = short.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsInt16(_reader);
			return true;
		}

		public bool ReadNextArgumentAsUInt32(out uint value)
		{
			if (EndOfStream)
			{
				value = uint.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsUInt32(_reader);
			return true;
		}

		public bool ReadNextArgumentAsInt32(out int value)
		{
			if (EndOfStream)
			{
				value = int.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsInt32(_reader);
			return true;
		}

		public bool ReadNextArgumentAsUInt64(out ulong value)
		{
			if (EndOfStream)
			{
				value = ulong.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsUInt64(_reader);
			return true;
		}

		public bool ReadNextArgumentAsInt64(out long value)
		{
			if (EndOfStream)
			{
				value = long.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsInt64(_reader);
			return true;
		}

		public bool ReadNextArgumentAsSingle(out float value)
		{
			if (EndOfStream)
			{
				value = float.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsSingle(_reader);
			return true;
		}

		public bool ReadNextArgumentAsDouble(out double value)
		{
			if (EndOfStream)
			{
				value = double.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsDouble(_reader);
			return true;
		}

		public bool ReadNextArgumentAsDecimal(out decimal value)
		{
			if (EndOfStream)
			{
				value = decimal.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsDecimal(_reader);
			return true;
		}

		public bool ReadNextArgumentAsDateTime(out DateTime value)
		{
			if (EndOfStream)
			{
				value = DateTime.MinValue;
				return false;
			}

			value = DateTime.FromBinary(_reader.ReadInt64());
			return true;
		}

		public bool ReadNextArgumentAsString(out string value)
		{
			if (EndOfStream)
			{
				value = null;
				return false;
			}

			value = BinarySerializer2.ReadValueAsString(_reader);
			return true;
		}

		public bool ReadNextArgumentAsBytes(out byte[] value)
		{
			if (EndOfStream)
			{
				value = null;
				return false;
			}

			if (_reader.ReadBoolean())
			{
				int length = _reader.ReadInt32();
				value = _reader.ReadBytes(length);
			}
			else
			{
				value = null;
			}

			return true;
		}
	}
}