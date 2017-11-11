using System;
using System.IO;

namespace SharpRemote.CodeGeneration.Serialization.Json
{
	internal sealed class JsonMethodInvocationWriter
		: IMethodInvocationWriter
	{
		public JsonMethodInvocationWriter(Stream stream, ulong grainId, string methodName, ulong rpcId)
		{
			throw new NotImplementedException();
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(object value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument<T>(T value) where T : struct
		{
			throw new NotImplementedException();
		}

		public void WriteArgument<T>(ref T value) where T : struct
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(sbyte value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(byte value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(ushort value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(short value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(uint value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(int value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(ulong value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(long value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(float value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(double value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(decimal value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(string value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(byte[] value)
		{
			throw new NotImplementedException();
		}
	}
}