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

		public bool ReadNextArgument(out string argumentName, out object value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsStruct<T>(out string name, out T value) where T : struct
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsSByte(out string name, out sbyte value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsByte(out string name, out byte value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsUInt16(out string name, out ushort value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsInt16(out string name, out short value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsUInt32(out string name, out uint value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsInt32(out string name, out int value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsUInt64(out string name, out ulong value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsInt64(out string name, out long value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsFloat(out string name, out float value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsDouble(out string name, out double value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsDecimal(out string name, out decimal value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsString(out string name, out string value)
		{
			throw new NotImplementedException();
		}

		public bool ReadNextArgumentAsBytes(out string name, out byte[] value)
		{
			throw new NotImplementedException();
		}
	}
}