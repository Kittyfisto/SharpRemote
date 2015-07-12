﻿using System;
using System.Net;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a connection to a non-existing / unreachable endpoint is established.
	/// </summary>
	[Serializable]
	public sealed class NoSuchIPEndPointException
		: RemotingException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public NoSuchIPEndPointException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			var ip = info.GetString("Address");
			IPAddress address;
			if (ip != null && IPAddress.TryParse(ip, out address))
			{
				int port = info.GetInt32("Port");
				EndPoint = new IPEndPoint(address, port);
			}
			EndPointName = info.GetString("EndPointName");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Address", EndPoint != null ? EndPoint.Address.ToString() : null);
			info.AddValue("Port", EndPoint != null ? EndPoint.Port : int.MaxValue);
			info.AddValue("EndPointName", EndPointName);
		}
#endif
#endif

		/// <summary>
		/// Initializes an instance of this exception with the given ipendpoint and inner exception
		/// that caused this exception.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="innerException"></param>
		public NoSuchIPEndPointException(IPEndPoint endPoint, Exception innerException = null)
			: base(string.Format("Unable to establish a connection with the given endpoint: {0}", endPoint), innerException)
		{
			EndPoint = endPoint;
		}

		/// <summary>
		/// Initializes an instance of this exception with the given endpoint name and inner exception
		/// that caused this exception.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="innerException"></param>
		public NoSuchIPEndPointException(string endPointName, Exception innerException = null)
			: base(string.Format("Unable to establish a connection with the given endpoint: {0}", endPointName), innerException)
		{
			EndPointName = endPointName;
		}

		/// <summary>
		/// 
		/// </summary>
		public NoSuchIPEndPointException()
		{
			
		}

		/// <summary>
		/// The ip-endpoint in question, if given.
		/// </summary>
		public readonly IPEndPoint EndPoint;

		/// <summary>
		/// The name of the endpoint in question, if given.
		/// </summary>
		public readonly string EndPointName;
	}
}