using System;
using System.Net;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	[Serializable]
	public sealed class NoSuchEndPointException
		: RemotingException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		public NoSuchEndPointException(SerializationInfo info, StreamingContext context)
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

		public NoSuchEndPointException(IPEndPoint endPoint, Exception e = null)
			: base(string.Format("Unable to establish a connection with the given endpoint: {0}", endPoint), e)
		{
			EndPoint = endPoint;
		}

		public readonly IPEndPoint EndPoint;
	}
}