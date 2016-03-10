using System.IO.Pipes;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public sealed class NamedPipeRemotingEndPointServer
		: AbstractNamedPipeEndPoint<NamedPipeServerStream>
	{
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