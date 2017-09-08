using System.IO;
using System.Net;
using System.Text;

namespace SharpRemote.ServiceDiscovery
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class BinaryWriterEx
		: BinaryWriter
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="output"></param>
		public BinaryWriterEx(Stream output) : base(output)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="output"></param>
		/// <param name="encoding"></param>
		public BinaryWriterEx(Stream output, Encoding encoding) : base(output, encoding)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="output"></param>
		/// <param name="encoding"></param>
		/// <param name="leaveOpen"></param>
		public BinaryWriterEx(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
		{
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="endPoint"></param>
		public void Write(IPEndPoint endPoint)
		{
			var addr = endPoint.Address.GetAddressBytes();
			Write((byte)addr.Length);
			Write(addr);
			Write(endPoint.Port);
		}
	}
}
