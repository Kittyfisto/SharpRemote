using System;
using System.IO;

// ReSharper disable once CheckNamespace
namespace SharpRemote
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

		public void WriteNamedArgument(string name, object value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, sbyte value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, byte value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, ushort value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, short value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, uint value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, int value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, ulong value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, long value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, float value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, double value)
		{
			throw new NotImplementedException();
		}

		public void WriteNamedArgument(string name, string value)
		{
			throw new NotImplementedException();
		}
	}
}