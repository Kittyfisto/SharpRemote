using System;
using System.IO;
using System.Text;

namespace SharpRemote.CodeGeneration.Serialization.Binary
{
	internal sealed class BinaryMethodResultWriter
		: IMethodResultWriter
	{
		private readonly BinaryWriter _writer;

		public BinaryMethodResultWriter(Stream stream, ulong rpcId)
		{
			_writer = new BinaryWriter(stream, Encoding.UTF8, true);
			_writer.Write(rpcId);
		}

		public void Dispose()
		{
			_writer.Dispose();
		}

		public void WriteFinished()
		{
			throw new NotImplementedException();
		}

		public void WriteResult(object value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(sbyte value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(byte value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(ushort value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(short value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(uint value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(int value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(ulong value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(long value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(float value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(double value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(string value)
		{
			throw new NotImplementedException();
		}

		public void WriteResult(byte[] value)
		{
			throw new NotImplementedException();
		}

		public void WriteException(Exception e)
		{
			throw new NotImplementedException();
		}
	}
}