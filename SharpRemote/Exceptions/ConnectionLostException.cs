using System;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This exception is thrown when a synchronous method on the proxy, or an event on the servant, is called and the
	///     connection
	///     has been interrupted while the call was ongoing.
	/// </summary>
	[Serializable]
	public class ConnectionLostException
		: RemoteProcedureCallCanceledException
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		///     The name of the endpoint who's connection dropped.
		/// </summary>
		public readonly string EndPointName;

		/// <summary>
		///     The address of the local endpoint which lost the connection.
		/// </summary>
		public readonly EndPoint LocalEndPoint;

		/// <summary>
		///     The address of the endpoint to which the connection has been lost.
		/// </summary>
		public readonly EndPoint RemoteEndPoint;

		/// <summary>
		///     Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public ConnectionLostException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			try
			{
				EndPointName = info.GetString("EndPointName");
				LocalEndPoint = info.GetValue("LocalEndPoint", typeof(EndPoint)) as EndPoint;
				RemoteEndPoint = info.GetValue("RemoteEndPoint", typeof(EndPoint)) as EndPoint;
			}
			catch (Exception e)
			{
				Log.WarnFormat("Unable to deserialize additional exception information: {0}", e);
			}
		}

		/// <summary>
		///     Initializes a new instance of this exception.
		/// </summary>
		public ConnectionLostException()
			: base("The connection to the remote endpoint has been lost")
		{
		}

		/// <summary>
		///     Initializes a new instance of this exception.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="localEndPoint"></param>
		/// <param name="remoteEndPoint"></param>
		public ConnectionLostException(string endPointName, EndPoint localEndPoint = null, EndPoint remoteEndPoint = null)
			: base("The connection to the remote endpoint has been lost")
		{
			EndPointName = endPointName;
			LocalEndPoint = localEndPoint;
			RemoteEndPoint = remoteEndPoint;
		}

		/// <inheritdoc />
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("EndPointName", EndPointName);
			info.AddValue("LocalEndPoint", LocalEndPoint);
			info.AddValue("RemoteEndPoint", RemoteEndPoint);
		}
	}
}