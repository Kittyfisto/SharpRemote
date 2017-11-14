using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodResultReader
		: IMethodResultReader
	{
		private readonly BinaryReader _reader;
		private readonly ulong _rpcId;

		public BinaryMethodResultReader(BinaryReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException(nameof(reader));

			_reader = reader;
			_rpcId = _reader.ReadUInt64();
		}

		public void Dispose()
		{
			_reader.Dispose();
		}

		public ulong RpcId => _rpcId;

		public bool ReadException(out Exception exception)
		{
			throw new NotImplementedException();
		}

		public bool ReadResult(out object value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultSByte(out sbyte value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultByte(out byte value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultUInt16(out ushort value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultInt16(out short value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultUInt32(out uint value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultInt32(out int value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultUInt64(out ulong value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultInt64(out long value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultSingle(out float value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultDouble(out double value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultString(out string value)
		{
			throw new NotImplementedException();
		}

		public bool ReadResultBytes(out byte[] value)
		{
			throw new NotImplementedException();
		}
	}
}