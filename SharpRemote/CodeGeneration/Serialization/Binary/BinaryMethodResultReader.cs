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