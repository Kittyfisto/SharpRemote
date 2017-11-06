using System;

namespace SharpRemote.CodeGeneration.Serialization.Xml
{
	internal sealed class XmlMethodInvocationReader
		: IMethodInvocationReader
	{
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

		public object ReadNextArgument(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public sbyte ReadNextArgumentAsSByte(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public byte ReadNextArgumentAsByte(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public ushort ReadNextArgumentAsUInt16(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public short ReadNextArgumentAsInt16(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public uint ReadNextArgumentAsUInt32(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public int ReadNextArgumentAsInt32(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public ulong ReadNextArgumentAsUInt64(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public long ReadNextArgumentAsInt64(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public float ReadNextArgumentAsFloat(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public double ReadNextArgumentAsDouble(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public string ReadNextArgumentAsString(out string argumentName)
		{
			throw new NotImplementedException();
		}

		public byte[] ReadNextArgumentAsBytes(out string argumentName)
		{
			throw new NotImplementedException();
		}
	}
}