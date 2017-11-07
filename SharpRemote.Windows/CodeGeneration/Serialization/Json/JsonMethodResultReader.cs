using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Json
{
	internal sealed class JsonMethodResultReader
		: IMethodResultReader
	{
		public JsonMethodResultReader(Stream stream)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public ulong RpcId
		{
			get { throw new NotImplementedException(); }
		}

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