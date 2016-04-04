using System;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This exception is thrown when a connection to a non-existing / unreachable endpoint is established.
	/// </summary>
	[Serializable]
	internal sealed class NoSuchNamedPipeEndPointException
		: SharpRemoteException
	{
#if !WINDOWS_PHONE_APP
#if !SILVERLIGHT
		/// <summary>
		/// Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public NoSuchNamedPipeEndPointException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			var type = (NamedPipeEndPoint.PipeType) info.GetByte("Type");
			if (type != NamedPipeEndPoint.PipeType.None)
			{
				EndPoint = new NamedPipeEndPoint(info.GetString("EndPoint"), type);
			}
			EndPointName = info.GetString("EndPointName");
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("EndPoint", EndPoint != null ? EndPoint.PipeName : null);
			info.AddValue("Type", EndPoint != null ? (byte) EndPoint.Type : (byte)NamedPipeEndPoint.PipeType.None);
			info.AddValue("EndPointName", EndPointName);
		}
#endif
#endif

		/// <summary>
		/// Initializes an instance of this exception with the given named pipe endpoint and inner exception
		/// that caused this exception.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="innerException"></param>
		public NoSuchNamedPipeEndPointException(NamedPipeEndPoint endPoint, Exception innerException = null)
			: base(string.Format("Unable to establish a connection with the given endpoint: {0}", endPoint), innerException)
		{
			EndPoint = endPoint;
		}

		/// <summary>
		/// Initializes an instance of this exception with the given named pipe endpoint and inner exception
		/// that caused this exception.
		/// </summary>
		/// <param name="endPoint"></param>
		/// <param name="timeout">The amount of time that passed until the connection-establishment was dropped</param>
		/// <param name="innerException"></param>
		public NoSuchNamedPipeEndPointException(NamedPipeEndPoint endPoint, TimeSpan timeout, Exception innerException = null)
			: base(
				string.Format("Unable to establish a connection with the given endpoint after {0}: {1}",
				              Format(timeout),
				              endPoint),
				innerException)
		{
			EndPoint = endPoint;
		}

		private static string Format(TimeSpan timeout)
		{
			if (timeout >= TimeSpan.FromHours(1))
				return string.Format("{0} hours", (int)timeout.TotalHours);

			if (timeout >= TimeSpan.FromMinutes(1))
				return string.Format("{0} minutes", (int)timeout.TotalMinutes);

			if (timeout >= TimeSpan.FromSeconds(1))
				return string.Format("{0} seconds", (int)timeout.TotalSeconds);

			return string.Format("{0} ms", (int)timeout.TotalMilliseconds);
		}

		/// <summary>
		/// Initializes an instance of this exception with the given endpoint name and inner exception
		/// that caused this exception.
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="innerException"></param>
		public NoSuchNamedPipeEndPointException(string endPointName, Exception innerException = null)
			: base(string.Format("Unable to establish a connection with the given endpoint: {0}", endPointName), innerException)
		{
			EndPointName = endPointName;
		}

		/// <summary>
		/// Initializes a new instance of this exception.
		/// </summary>
		public NoSuchNamedPipeEndPointException()
		{

		}

		/// <summary>
		/// The named pipe-endpoint in question, if given.
		/// </summary>
		public readonly NamedPipeEndPoint EndPoint;

		/// <summary>
		/// The name of the endpoint in question, if given.
		/// </summary>
		public readonly string EndPointName;
	}
}