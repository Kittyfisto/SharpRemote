using System;
using System.IO;
using System.Net;
using System.Text;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	///     An extension to <see cref="BinaryReader" />:
	///     Contains many methods to read values from the stream without throwing exceptions
	///     if the stream is too small.
	/// </summary>
	public sealed class BinaryReaderExt
		: BinaryReader
	{
		/// <summary>
		/// </summary>
		/// <param name="input"></param>
		public BinaryReaderExt(Stream input) : base(input)
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="input"></param>
		/// <param name="encoding"></param>
		public BinaryReaderExt(Stream input, Encoding encoding) : base(input, encoding)
		{
		}

		/// <summary>
		/// </summary>
		/// <param name="input"></param>
		/// <param name="encoding"></param>
		/// <param name="leaveOpen"></param>
		public BinaryReaderExt(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
		{
		}

		/// <summary>
		///     The amount of bytes left in the <see cref="BinaryReader.BaseStream" />.
		/// </summary>
		public long BytesLeft
		{
			get
			{
				var length = BaseStream.Length;
				var position = BaseStream.Position;
				return length - position;
			}
		}

		/// <summary>
		///     Tries to read a boolean value from the stream or returns false
		///     if there isn't enough bytes left.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryReadBoolean(out bool value)
		{
			if (BytesLeft < 1)
			{
				value = false;
				return false;
			}

			value = ReadBoolean();
			return true;
		}

		/// <summary>
		///     Tries to read a string from the stream or returns false
		///     if there isn't enough bytes left.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryRead(out string value)
		{
			if (BytesLeft < 4)
			{
				value = null;
				return false;
			}

			value = ReadString();
			return true;
		}

		/// <summary>
		///     Tries to read the given amount of bytes from the stream or returns false
		///     if there isn't enough bytes left.
		/// </summary>
		/// <param name="length"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryReadBytes(int length, out byte[] value)
		{
			if (BytesLeft < length)
			{
				value = null;
				return false;
			}

			value = ReadBytes(length);
			return true;
		}

		/// <summary>
		///     Tries to read an integer from the stream or returns false
		///     if there isn't enough bytes left.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryRead(out ushort value)
		{
			if (BytesLeft < 4)
			{
				value = ushort.MinValue;
				return false;
			}

			value = ReadUInt16();
			return true;
		}

		/// <summary>
		///     Tries to read an integer from the stream or returns false
		///     if there isn't enough bytes left.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryRead(out int value)
		{
			if (BytesLeft < 4)
			{
				value = Int32.MinValue;
				return false;
			}

			value = ReadInt32();
			return true;
		}

		/// <summary>
		///     Tries to read an integer from the stream or returns false
		///     if there isn't enough bytes left.
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public bool TryRead(out uint value)
		{
			if (BytesLeft < 4)
			{
				value = UInt32.MinValue;
				return false;
			}

			value = ReadUInt32();
			return true;
		}

		/// <summary>
		/// </summary>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		public bool TryRead(out IPEndPoint endPoint)
		{
			if (BytesLeft < 1)
			{
				endPoint = null;
				return false;
			}

			var length = ReadByte();
			byte[] bytes;
			if (!TryReadBytes(length, out bytes))
			{
				endPoint = null;
				return false;
			}

			int port;
			if (!TryRead(out port))
			{
				endPoint = null;
				return false;
			}

			var addr = new IPAddress(bytes);
			endPoint = new IPEndPoint(addr, port);
			return true;
		}
	}
}