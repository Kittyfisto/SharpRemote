using System;
using System.Net;
using System.Runtime.Serialization;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This exception is thrown when an endpoint (client) tries to connect to another remote (server)
	///     end point and the latter does not accept the client's connection because it already is connected
	///     to a different client.
	/// </summary>
	[Serializable]
	public class RemoteEndpointAlreadyConnectedException
		: SharpRemoteException
	{
		/// <summary>
		///     Initializes a new instance of this exception.
		/// </summary>
		public RemoteEndpointAlreadyConnectedException()
		{
		}

		/// <summary>
		///     Deserialization ctor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		public RemoteEndpointAlreadyConnectedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			BlockingEndPoint = info.GetValue("BlockingEndPoint", typeof(EndPoint)) as EndPoint;
		}

		/// <summary>
		///     Initializes a new instance with the given message and inner exception that is the cause
		///     for this exception.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="blockingEndPoint">The endpoint responsible for not being able to establish a connection</param>
		/// <param name="innerException"></param>
		public RemoteEndpointAlreadyConnectedException(string message,
		                                               EndPoint blockingEndPoint,
		                                               Exception innerException = null)
			: base(message, innerException)
		{
			BlockingEndPoint = blockingEndPoint;
		}

		/// <summary>
		///     The endpoint responsible for not being able to establish a connection.
		/// </summary>
		public EndPoint BlockingEndPoint { get; set; }

		/// <inheritdoc />
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);

			info.AddValue("BlockingEndPoint", BlockingEndPoint);
		}
	}
}