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

		public Exception ReadException()
		{
			throw new NotImplementedException();
		}

		public object ReadResult()
		{
			throw new NotImplementedException();
		}

		public sbyte ReadResultAsSByte()
		{
			throw new NotImplementedException();
		}

		public byte ReadResultAsByte()
		{
			throw new NotImplementedException();
		}

		public ushort ReadResultAsUInt16()
		{
			throw new NotImplementedException();
		}

		public short ReadResultAsInt16()
		{
			throw new NotImplementedException();
		}

		public uint ReadResultUInt32()
		{
			throw new NotImplementedException();
		}

		public int ReadResultAsInt32()
		{
			throw new NotImplementedException();
		}

		public ulong ReadResultAsUInt64()
		{
			throw new NotImplementedException();
		}

		public long ReadResultAsInt64()
		{
			throw new NotImplementedException();
		}

		public float ReadResultAsFloat()
		{
			throw new NotImplementedException();
		}

		public double ReadResultAsDouble()
		{
			throw new NotImplementedException();
		}

		public string ReadResultAsString()
		{
			throw new NotImplementedException();
		}

		public byte[] ReadResultAsBytes()
		{
			throw new NotImplementedException();
		}
	}
}