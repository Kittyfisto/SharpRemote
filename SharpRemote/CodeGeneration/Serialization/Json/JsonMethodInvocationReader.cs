using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Json
{
	internal sealed class JsonMethodInvocationReader
		: IMethodInvocationReader
	{
		public JsonMethodInvocationReader(Stream stream)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public ulong GrainId
		{
			get { throw new NotImplementedException(); }
		}

		public string MethodName
		{
			get { throw new NotImplementedException(); }
		}

		public ulong RpcId
		{
			get { throw new NotImplementedException(); }
		}

		public bool ReadNextArgument(out object value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsStruct<T>(out T value) where T : struct
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsSByte(out sbyte value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsByte(out byte value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsUInt16(out ushort value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsInt16(out short value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsUInt32(out uint value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsInt32(out int value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsUInt64(out ulong value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsInt64(out long value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsFloat(out float value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsDouble(out double value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsDecimal(out decimal value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsString(out string value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsBytes(out byte[] value)
		{
			throw new NotImplementedException();
		}
	}
}