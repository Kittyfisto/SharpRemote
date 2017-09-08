using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace SharpRemote.ServiceDiscovery
{
	internal static class Message
	{
		public const string P2PQueryToken = "SharpRemote.P2P.Query";

		/// <summary>
		///     Token to identify a response from software using SharpRemote V0.3 or earlier.
		/// </summary>
		public const string P2PResponseLegacyToken = "SharpRemote.P2P.Response";

		/// <summary>
		///     Token to identify a response from software using SharpRemote V0.4 or later.
		/// </summary>
		/// <remarks>
		///     This is the preferred response to use because it is forward and backwards compatible.
		/// </remarks>
		public const string P2PResponse2Token = "SharpRemote.P2P.Response2";

		/// <summary>
		///     The maximum allowed length of a network discovery message.
		/// </summary>
		/// <remarks>
		///     This limit exists in order to follow the guideline that a UDP packet shouldn't
		///     be greater than 512 in order to have good chances of getting reassembled
		///     in case the IPv4 MTU of any intermediate host is exceeded.
		/// </remarks>
		public const int MaximumMessageLength = 512;

		[Pure]
		public static byte[] CreateQuery(string name)
		{
			using (var md5 = MD5.Create())
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				writer.Write(P2PQueryToken);
				writer.Write(name);
				writer.Flush();

				var hash = md5.ComputeHash(stream.GetBuffer(), offset: 0, count: (int) stream.Length);
				writer.Write(hash);

				return stream.ToArray();
			}
		}

		[Pure]
		public static byte[] CreateLegacyResponse(string name, IPEndPoint endPoint)
		{
			using (var md5 = MD5.Create())
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriterEx(stream))
			{
				writer.Write(P2PResponseLegacyToken);
				writer.Write(name);
				writer.Write(endPoint);
				writer.Flush();

				var hash = md5.ComputeHash(stream.GetBuffer(), offset: 0, count: (int) stream.Position);
				writer.Write(hash);

				return stream.ToArray();
			}
		}

		[Pure]
		public static byte[] CreateResponse2(string name, IPEndPoint endPoint, byte[] payload = null)
		{
			using (var md5 = MD5.Create())
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriterEx(stream))
			{
				writer.Write(P2PResponse2Token);
				writer.Flush();
				var contentLengthStart = (int) stream.Position;
				writer.Write((ushort) 0); //< placeholder for message length
				var hashStart = stream.Position;
				writer.Write(new byte[16]); //< placeholder for message hash
				var content = MessageContent.Name | MessageContent.EndPoint;
				if (payload != null)
					content |= MessageContent.Payload;
				writer.Write((uint) content);

				writer.Write(name);
				writer.Write(endPoint);
				if (payload != null)
				{
					writer.Write((ushort) payload.Length);
					writer.Write(payload);
				}
				writer.Flush();

				var contentLength = (int) (stream.Position - contentLengthStart - 2);
				stream.Position = contentLengthStart;
				writer.Write(contentLength);
				writer.Flush();

				var hash = md5.ComputeHash(stream.GetBuffer(), contentLengthStart, contentLength + 2);
				stream.Position = hashStart;
				writer.Write(hash);

				var message = stream.ToArray();
				if (message.Length > MaximumMessageLength)
					throw new ArgumentOutOfRangeException(nameof(payload),
						string.Format("The total size of a message may not exceed {0} bytes (this message would be {1} bytes in length)",
							MaximumMessageLength,
							message.Length));

				return message;
			}
		}

		public static bool TryRead(byte[] message, out string token, out string name, out IPEndPoint endPoint,
			out byte[] payload)
		{
			token = null;
			name = null;
			endPoint = null;
			payload = null;
			if (message.Length == 0)
				return false;

			using (var md5 = MD5.Create())
			using (var stream = new MemoryStream(message))
			using (var reader = new BinaryReaderExt(stream))
			{
				if (!reader.TryRead(out token))
					return false;

				if (token == P2PQueryToken)
					return ReadQuery(ref name, reader, md5, stream);
				if (token == P2PResponse2Token)
					return ReadResponse2(ref name, ref endPoint, ref payload, reader, md5, stream);
				if (token == P2PResponseLegacyToken)
					return ReadLegacyResponse(ref name, ref endPoint, reader, md5, stream);

				return false;
			}
		}

		private static bool ReadResponse2(ref string name, ref IPEndPoint endPoint, ref byte[] payload,
			BinaryReaderExt reader, MD5 md5, MemoryStream stream)
		{
			// This code is intended to be forwards compatible.
			// This means that it needs to be able to deal with content types which
			// it does simply not know (yet). Obviously such content would not
			// be decoded, however it is super important that we do not discard future
			// responses just because we calculated the hash wrong.
			// For this reason, we include the total message length here, so that even if we
			// do not know the additional content, we include it in our hash code calculation.
			var contentLengthStart = (int) stream.Position;
			ushort contentLength;
			if (!reader.TryRead(out contentLength))
				return false;

			if (reader.BytesLeft < contentLength)
				return false;

			var hashStart = (int) reader.BaseStream.Position;
			var hash = reader.ReadBytes(count: 16);
			var content = (MessageContent) reader.ReadInt32();
			if ((content & MessageContent.Name) == MessageContent.Name)
				if (!reader.TryRead(out name))
					return false;
			if ((content & MessageContent.EndPoint) == MessageContent.EndPoint)
				if (!reader.TryRead(out endPoint))
					return false;
			if ((content & MessageContent.Payload) == MessageContent.Payload)
			{
				int payloadLength = reader.ReadUInt16();
				payload = reader.ReadBytes(payloadLength);
			}

			// We need to black out the actual hash to 0 before
			// we compute the hash of the received content
			stream.Position = hashStart;
			for (var i = 0; i < 16; ++i)
				stream.WriteByte(value: 0);
			var actualHash = md5.ComputeHash(stream.ToArray(), contentLengthStart, contentLength + 2);
			if (!AreHashesEqual(hash, actualHash))
				return false;

			return true;
		}

		private static bool ReadLegacyResponse(ref string name, ref IPEndPoint endPoint, BinaryReaderExt reader, MD5 md5,
			MemoryStream stream)
		{
			if (!reader.TryRead(out name))
				return false;

			if (!reader.TryRead(out endPoint))
				return false;

			if (!IsMessageAuthentic(md5, stream, reader))
			{
				name = null;
				endPoint = null;
				return false;
			}

			return true;
		}

		private static bool ReadQuery(ref string name, BinaryReaderExt reader, MD5 md5, MemoryStream stream)
		{
			if (!reader.TryRead(out name))
				return false;

			if (!IsMessageAuthentic(md5, stream, reader))
			{
				name = null;
				return false;
			}

			return true;
		}

		private static bool IsMessageAuthentic(
			MD5 md5,
			MemoryStream stream,
			BinaryReader reader)
		{
			var messageLength = (int) stream.Position;
			var sentHash = new byte[16];
			if (reader.Read(sentHash, index: 0, count: sentHash.Length) != sentHash.Length)
				return false;

			var actualHash = md5.ComputeHash(stream.ToArray(), offset: 0, count: messageLength);
			if (!AreHashesEqual(sentHash, actualHash))
				return false;

			return true;
		}

		private static bool AreHashesEqual(byte[] sentHash, byte[] actualHash)
		{
			if (sentHash.Length != actualHash.Length)
				return false;

			for (var i = 0; i < sentHash.Length; ++i)
				if (sentHash[i] != actualHash[i])
					return false;

			return true;
		}

		[Flags]
		private enum MessageContent : uint
		{
			None = 0,

			Name = 0x00000001,

			EndPoint = 0x00000002,

			Payload = 0x80000000
		}
	}
}