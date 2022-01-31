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
		public ConnectionLostException(string endPointName)
			: base("The connection to the remote endpoint has been lost")
		{
			EndPointName = endPointName;
		}

		/// <inheritdoc />
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("EndPointName", EndPointName);
		}
	}
}