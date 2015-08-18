using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Security.Cryptography;

namespace SharpRemote.Broadcasting
{
	internal static class Message
	{
		public const string P2PQueryToken = "SharpRemote.P2P.Query";
		public const string P2PResponseToken = "SharpRemote.P2P.Response";

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

				var hash = md5.ComputeHash(stream.GetBuffer(), 0, (int)stream.Length);
				writer.Write(hash);

				return stream.ToArray();
			}
		}

		[Pure]
		public static byte[] CreateResponse(string name, IPEndPoint endPoint)
		{
			using (var md5 = MD5.Create())
			using (var stream = new MemoryStream())
			using (var writer = new BinaryWriter(stream))
			{
				writer.Write(P2PResponseToken);
				writer.Write(name);
				var addr = endPoint.Address.GetAddressBytes();
				writer.Write((byte)addr.Length);
				writer.Write(addr);
				writer.Write(endPoint.Port);
				writer.Flush();

				var hash = md5.ComputeHash(stream.GetBuffer(), 0, (int)stream.Position);
				writer.Write(hash);

				return stream.ToArray();
			}
		}

		private static bool TryCompareHashes(
			MD5 md5,
			MemoryStream stream,
			BinaryReader reader)
		{
			var messageLength = (int)stream.Position;
			var sentHash = new byte[16];
			if (reader.Read(sentHash, 0, sentHash.Length) != sentHash.Length)
			{
				return false;
			}

			byte[] actualHash = md5.ComputeHash(stream.ToArray(), 0, messageLength);
			if (!CompareHashes(sentHash, actualHash))
			{
				return false;
			}

			return true;
		}

		public static bool TryRead(byte[] message, EndPoint remoteEndPoint, out string token, out string name, out IPEndPoint endPoint)
		{
			using (var md5 = MD5.Create())
			using (var stream = new MemoryStream(message))
			using (var reader = new BinaryReader(stream))
			{
				token = reader.ReadString();
				if (token == P2PQueryToken)
				{
					name = reader.ReadString();
					endPoint = null;

					if (!TryCompareHashes(md5, stream, reader))
					{
						name = null;
						return false;
					}

					return true;
				}
				if (token == P2PResponseToken)
				{
					name = reader.ReadString();
					var length = reader.ReadByte();
					var bytes = reader.ReadBytes(length);
					var port = reader.ReadInt32();
					var addr = new IPAddress(bytes);
					endPoint = new IPEndPoint(addr, port);

					if (!TryCompareHashes(md5, stream, reader))
					{
						name = null;
						endPoint = null;
						return false;
					}

					// It's possible the sender publishes his service on more than one interface, in which case
					// IPAddress.Any is passed - we have to patch the response with the address of the endpoint that
					// sent us this response...
					var remoteIPEndPoint = remoteEndPoint as IPEndPoint;
					if (Equals(endPoint.Address, IPAddress.Any) || Equals(endPoint.Address, IPAddress.IPv6Any))
					{
						if (remoteIPEndPoint != null)
						{
							endPoint = new IPEndPoint(
								remoteIPEndPoint.Address,
								endPoint.Port
								);
						}
					}

					return true;
				}

				name = null;
				endPoint = null;
				return false;
			}
		}

		private static bool CompareHashes(byte[] sentHash, byte[] actualHash)
		{
			if (sentHash.Length != actualHash.Length)
				return false;

			for (int i = 0; i < sentHash.Length; ++i)
			{
				if (sentHash[i] != actualHash[i])
					return false;
			}

			return true;
		}
	}
}