using System;
using System.IO;
using System.Text;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodCallWriter
		: IMethodCallWriter
	{
		private readonly BinaryWriter _writer;

		public BinaryMethodCallWriter(Stream stream, ulong grainId, string methodName, ulong rpcId)
		{
			_writer = new BinaryWriter(stream, Encoding.UTF8, true);
			_writer.Write((byte)MessageType2.Call);
			_writer.Write(grainId);
			_writer.Write(methodName);
			_writer.Write(rpcId);
		}

		public void Dispose()
		{
			_writer.Dispose();
		}

		public void WriteArgument(object value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(sbyte value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(byte value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(ushort value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(short value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(uint value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(int value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(ulong value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(long value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(float value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(double value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(decimal value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(DateTime value)
		{
			throw new NotImplementedException();
		}

		public void WriteArgument(string value)
		{
			_writer.Write(value);
		}

		public void WriteArgument(byte[] value)
		{
			_writer.Write(value.Length);
			_writer.Write(value);
		}
	}
}