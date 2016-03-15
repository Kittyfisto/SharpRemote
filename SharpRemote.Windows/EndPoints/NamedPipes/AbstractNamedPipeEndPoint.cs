using System.IO.Pipes;
using System.Net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="TTransport"></typeparam>
	public abstract class AbstractNamedPipeEndPoint<TTransport>
		: AbstractBinaryStreamEndPoint<TTransport>
		where TTransport : PipeStream
	{
		private NamedPipeEndPoint _localEndPoint;
		private NamedPipeEndPoint _remoteEndPoint;

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

		/// <summary>
		/// 
		/// </summary>
		public new NamedPipeEndPoint LocalEndPoint
		{
			get { return _localEndPoint; }
			protected set { _localEndPoint = value; }
		}

		/// <summary>
		/// 
		/// </summary>
		public new NamedPipeEndPoint RemoteEndPoint
		{
			get { return _remoteEndPoint; }
		}

		protected override EndPoint InternalLocalEndPoint
		{
			get { return _localEndPoint; }
		}

		protected override EndPoint InternalRemoteEndPoint
		{
			get { return _remoteEndPoint; }
			set { _remoteEndPoint = (NamedPipeEndPoint)value; }
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