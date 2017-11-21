using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodCallReader
		: IMethodCallReader
	{
		private readonly Stream _stream;
		private readonly BinaryReader _reader;
		private readonly ulong _grainId;
		private readonly string _methodName;
		private readonly ulong _rpcId;

		public BinaryMethodCallReader(BinaryReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

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
			
			throw new NotImplementedException();
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

			value = _reader.ReadSByte();
			return true;
		}

		public bool ReadNextArgumentAsByte(out byte value)
		{
			if (EndOfStream)
			{
				value = byte.MinValue;
				return false;
			}

			value = _reader.ReadByte();
			return true;
		}

		public bool ReadNextArgumentAsUInt16(out ushort value)
		{
			if (EndOfStream)
			{
				value = ushort.MinValue;
				return false;
			}

			value = _reader.ReadUInt16();
			return true;
		}

		public bool ReadNextArgumentAsInt16(out short value)
		{
			if (EndOfStream)
			{
				value = short.MinValue;
				return false;
			}

			value = _reader.ReadInt16();
			return true;
		}

		public bool ReadNextArgumentAsUInt32(out uint value)
		{
			if (EndOfStream)
			{
				value = uint.MinValue;
				return false;
			}

			value = _reader.ReadUInt32();
			return true;
		}

		public bool ReadNextArgumentAsInt32(out int value)
		{
			if (EndOfStream)
			{
				value = int.MinValue;
				return false;
			}

			value = _reader.ReadInt32();
			return true;
		}

		public bool ReadNextArgumentAsUInt64(out ulong value)
		{
			if (EndOfStream)
			{
				value = ulong.MinValue;
				return false;
			}

			value = _reader.ReadUInt64();
			return true;
		}

		public bool ReadNextArgumentAsInt64(out long value)
		{
			if (EndOfStream)
			{
				value = long.MinValue;
				return false;
			}

			value = _reader.ReadInt64();
			return true;
		}

		public bool ReadNextArgumentAsSingle(out float value)
		{
			if (EndOfStream)
			{
				value = float.MinValue;
				return false;
			}

			value = _reader.ReadSingle();
			return true;
		}

		public bool ReadNextArgumentAsDouble(out double value)
		{
			if (EndOfStream)
			{
				value = double.MinValue;
				return false;
			}

			value = _reader.ReadDouble();
			return true;
		}

		public bool ReadNextArgumentAsDecimal(out decimal value)
		{
			if (EndOfStream)
			{
				value = decimal.MinValue;
				return false;
			}

			value = _reader.ReadDecimal();
			return true;
		}

		public bool ReadNextArgumentAsDateTime(out DateTime value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsString(out string value)
		{
			if (EndOfStream)
			{
				value = null;
				return false;
			}

			value = _reader.ReadString();
			return true;
		}

		public bool ReadNextArgumentAsBytes(out byte[] value)
		{
			if (EndOfStream)
			{
				value = null;
				return false;
			}

			int length = _reader.ReadInt32();
			value = _reader.ReadBytes(length);
			return true;
		}
	}
}