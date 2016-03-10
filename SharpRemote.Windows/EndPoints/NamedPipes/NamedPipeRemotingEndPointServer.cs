using System;
using System.IO.Pipes;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public sealed class NamedPipeRemotingEndPointServer
		: AbstractNamedPipeEndPoint<NamedPipeServerStream>
	{
		private NamedPipeServerStream _socket;

		public NamedPipeRemotingEndPointServer(string name,
		                                       IAuthenticator clientAuthenticator,
		                                       IAuthenticator serverAuthenticator,
		                                       ITypeResolver customTypeResolver,
		                                       Serializer serializer,
		                                       HeartbeatSettings heartbeatSettings,
		                                       LatencySettings latencySettings,
		                                       EndPointSettings endPointSettings)
			: base(name, EndPointType.Server,
			       clientAuthenticator,
			       serverAuthenticator,
			       customTypeResolver,
			       serializer,
			       heartbeatSettings,
			       latencySettings,
			       endPointSettings)
		{
		}

		/// <summary>
		/// Binds this endpoint to the given name.
		/// Once bound, incoming connections may be accepted.
		/// </summary>
		/// <param name="name"></param>
		public void Bind(string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("name");
			if (LocalEndPoint != null)
				throw new InvalidOperationException("This endpoint is already bound");

			_socket = new NamedPipeServerStream(name);
		}

		protected override void DisposeAdditional()
		{
			throw new System.NotImplementedException();
		}

		protected override void DisconnectTransport(NamedPipeServerStream socket, bool reuseSocket)
		{
			socket.Disconnect();
		}
	}
}