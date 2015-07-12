using System;
using System.Net;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a connection to an endpoint was established,
	/// but the endpoint did not adhere to protocol.
	/// </summary>
	[Serializable]
	public sealed class InvalidIPEndPointException
		: RemotingException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public InvalidIPEndPointException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			var ip = info.GetString("Address");
			IPAddress address;
			if (ip != null && IPAddress.TryParse(ip, out address))
			{
				int port = info.GetInt32("Port");
				EndPoint = new IPEndPoint(address, port);
			}
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Address", EndPoint != null ? EndPoint.Address.ToString() : null);
			info.AddValue("Port", EndPoint != null ? EndPoint.Port : int.MaxValue);
		}
#endif
#endif

		/// <summary>
		/// Initializes a new instance of this exception with the endpoint that the connection was established to.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="innerException"></param>
		public InvalidIPEndPointException(IPEndPoint endPoint, Exception innerException = null)
			: base(string.Format("The given socket does not represent a valid endpoint: {0}", endPoint), innerException)
		{
			EndPoint = endPoint;
		}

		/// <summary>
		/// The endpoint in question.
		/// </summary>
		public readonly IPEndPoint EndPoint;
	}
}