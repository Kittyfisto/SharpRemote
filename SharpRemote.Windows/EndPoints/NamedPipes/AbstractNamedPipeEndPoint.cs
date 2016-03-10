using System.IO.Pipes;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public abstract class AbstractNamedPipeEndPoint<TTransport>
		: AbstractBinaryStreamEndPoint<TTransport>
		where TTransport : PipeStream
	{
		internal AbstractNamedPipeEndPoint(string name,
			EndPointType type,
		                                   IAuthenticator clientAuthenticator,
			IAuthenticator serverAuthenticator,
		                                   ITypeResolver customTypeResolver,
			Serializer serializer,
		                                   HeartbeatSettings heartbeatSettings,
			LatencySettings latencySettings,
		                                   EndPointSettings endPointSettings)
			: base(
				new GrainIdGenerator(type), name, type, clientAuthenticator, serverAuthenticator, customTypeResolver, serializer, heartbeatSettings,
				latencySettings, endPointSettings)
		{
		}

		protected override System.Net.EndPoint GetRemoteEndPointOf(TTransport socket)
		{
			throw new System.NotImplementedException();
		}

		protected override System.Net.EndPoint InternalLocalEndPoint
		{
			get { throw new System.NotImplementedException(); }
		}

		protected override System.Net.EndPoint InternalRemoteEndPoint
		{
			get
			{
				throw new System.NotImplementedException();
			}
			set
			{
				throw new System.NotImplementedException();
			}
		}

		protected override ConnectionId OnHandshakeSucceeded(TTransport socket)
		{
			throw new System.NotImplementedException();
		}

		protected override void Send(TTransport socket, byte[] data, int offset, int size)
		{
			throw new System.NotImplementedException();
		}

		protected override bool SynchronizedRead(TTransport socket, byte[] buffer, out System.Net.Sockets.SocketError err)
		{
			throw new System.NotImplementedException();
		}

		protected override bool SynchronizedRead(TTransport socket, byte[] buffer, System.TimeSpan timeout, out System.Net.Sockets.SocketError err)
		{
			throw new System.NotImplementedException();
		}

		protected override bool SynchronizedWrite(TTransport socket, byte[] data, int length, out System.Net.Sockets.SocketError err)
		{
			throw new System.NotImplementedException();
		}
	}
}