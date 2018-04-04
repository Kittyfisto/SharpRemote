using System;
using System.Reflection;
using System.Runtime.Serialization;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This exception is thrown when a method is called on a proxy and the underlying connection
	///     to where its subject lies has is not connected (either because no connection attempt was made,
	///     or the connection was disconnected / dropped **before** the method call was invoked).
	/// </summary>
	/// <remarks>
	///     When this exception is thrown then it is guarantueed that the method on the subject 
	/// </remarks>
	[Serializable]
	public class NotConnectedException
		: RemoteProcedureCallCanceledException
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		///     The name of the endpoint that was not connected.
		/// </summary>
		public readonly string EndPointName;

		/// <summary>
		///     Initializes a new instance of this exception with the given endpoint name.
		/// </summary>
		/// <param name="endPointName"></param>
		public NotConnectedException(string endPointName)
			: base("This endpoint is not connected to any other endpoint")
		{
			EndPointName = endPointName;
		}

		/// <summary>
		///     Initializes a new instance of this exception.
		/// </summary>
		public NotConnectedException()
		{
		}

		/// <summary>
		///     Deserilization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public NotConnectedException(SerializationInfo info, StreamingContext context)
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

		/// <inheritdoc />
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("EndPointName", EndPointName);
		}
	}
}