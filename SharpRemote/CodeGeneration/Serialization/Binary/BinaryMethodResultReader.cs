using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodResultReader
		: IMethodResultReader
	{
		private readonly BinarySerializer2 _serializer;
		private readonly BinaryReader _reader;
		private readonly ulong _rpcId;
		private readonly Stream _stream;

		public BinaryMethodResultReader(BinarySerializer2 serializer, BinaryReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			_serializer = serializer;
			_reader = reader;
			_stream = reader.BaseStream;
			_rpcId = _reader.ReadUInt64();
		}
		
		private bool EndOfStream => _stream.Position >= _stream.Length;

		public void Dispose()
		{
			_reader.Dispose();
		}

		public ulong RpcId => _rpcId;

		public bool ReadException(out Exception exception)
		{
			if (EndOfStream)
			{
				exception = null;
				return false;
			}

			exception = BinarySerializer2.ReadValueAsException(_reader);
			return false;
		}

		public bool ReadResult(out object value)
		{
			if (EndOfStream)
			{
				value = null;
				return false;
			}

			value = _serializer.ReadObject(_reader);
			return true;
		}

		public bool ReadResultSByte(out sbyte value)
		{
			if (EndOfStream)
			{
				value = SByte.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsSByte(_reader);
			return true;
		}

		public bool ReadResultByte(out byte value)
		{
			if (EndOfStream)
			{
				value = byte.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsByte(_reader);
			return true;
		}

		public bool ReadResultUInt16(out ushort value)
		{
			if (EndOfStream)
			{
				value = ushort.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsUInt16(_reader);
			return true;
		}

		public bool ReadResultInt16(out short value)
		{
			if (EndOfStream)
			{
				value = short.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsInt16(_reader);
			return true;
		}

		public bool ReadResultUInt32(out uint value)
		{
			if (EndOfStream)
			{
				value = uint.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsUInt32(_reader);
			return true;
		}

		public bool ReadResultInt32(out int value)
		{
			if (EndOfStream)
			{
				value = int.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsInt32(_reader);
			return true;
		}

		public bool ReadResultUInt64(out ulong value)
		{
			if (EndOfStream)
			{
				value = ulong.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsUInt64(_reader);
			return true;
		}

		public bool ReadResultInt64(out long value)
		{
			if (EndOfStream)
			{
				value = long.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsInt64(_reader);
			return true;
		}

		public bool ReadResultSingle(out float value)
		{
			if (EndOfStream)
			{
				value = float.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsSingle(_reader);
			return true;
		}

		public bool ReadResultDouble(out double value)
		{
			if (EndOfStream)
			{
				value = float.MinValue;
				return false;
			}

			value = BinarySerializer2.ReadValueAsDouble(_reader);
			return true;
		}

		public bool ReadResultString(out string value)
		{
			if (EndOfStream)
			{
				value = null;
				return false;
			}

			value = BinarySerializer2.ReadValueAsString(_reader);
			return true;
		}

		public bool ReadResultBytes(out byte[] value)
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