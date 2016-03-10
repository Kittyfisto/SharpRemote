using System.IO.Pipes;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	public sealed class NamedPipeRemotingEndPointClient
		: AbstractNamedPipeEndPoint<NamedPipeClientStream>
	{
		public NamedPipeRemotingEndPointClient(string name,
		                                       IAuthenticator clientAuthenticator,
		                                       IAuthenticator serverAuthenticator,
		                                       ITypeResolver customTypeResolver,
		                                       Serializer serializer,
		                                       HeartbeatSettings heartbeatSettings,
		                                       LatencySettings latencySettings,
		                                       EndPointSettings endPointSettings)
			: base(name, EndPointType.Client,
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

		protected override void DisconnectTransport(NamedPipeClientStream socket, bool reuseSocket)
		{
			socket.Dispose();
		}
	}
}