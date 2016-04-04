using System;
using System.Net;
using System.Net.Sockets;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	internal sealed class WebRemotingEndPoint
		: AbstractBinaryStreamEndPoint<IDisposable>
	{
		public WebRemotingEndPoint(GrainIdGenerator idGenerator, string name, EndPointType type,
		                           IAuthenticator clientAuthenticator, IAuthenticator serverAuthenticator,
		                           ITypeResolver customTypeResolver, Serializer serializer,
		                           HeartbeatSettings heartbeatSettings, LatencySettings latencySettings,
		                           EndPointSettings endPointSettings)
			: base(
				idGenerator, name, type, clientAuthenticator, serverAuthenticator, customTypeResolver, serializer, heartbeatSettings,
				latencySettings, endPointSettings)
		{
		}

		protected override EndPoint InternalLocalEndPoint
		{
			get { throw new NotImplementedException(); }
		}

		protected override EndPoint InternalRemoteEndPoint
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}

		protected override void DisposeAdditional()
		{
			throw new NotImplementedException();
		}

		protected override void DisconnectTransport(IDisposable socket, bool reuseSocket)
		{
			throw new NotImplementedException();
		}

		protected override void DisposeAfterDisconnect(IDisposable socket)
		{
			throw new NotImplementedException();
		}

		protected override bool SendGoodbye(IDisposable socket, long waitTime, TimeSpan timeSpan)
		{
			throw new NotImplementedException();
		}

		protected override void Send(IDisposable socket, byte[] data, int offset, int size)
		{
			throw new NotImplementedException();
		}

		protected override EndPoint GetRemoteEndPointOf(IDisposable socket)
		{
			throw new NotImplementedException();
		}

		protected override bool SynchronizedWrite(IDisposable socket, byte[] data, int length, out SocketError err)
		{
			throw new NotImplementedException();
		}

		protected override bool SynchronizedRead(IDisposable socket, byte[] buffer, TimeSpan timeout, out SocketError err)
		{
			throw new NotImplementedException();
		}

		protected override bool SynchronizedRead(IDisposable socket, byte[] buffer, out SocketError err)
		{
			throw new NotImplementedException();
		}
	}
}