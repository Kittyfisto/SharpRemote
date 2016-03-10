using System.IO.Pipes;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class NamedPipeRemotingEndPointClient
		: AbstractNamedPipeEndPoint<NamedPipeClientStream>
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="clientAuthenticator"></param>
		/// <param name="serverAuthenticator"></param>
		/// <param name="customTypeResolver"></param>
		/// <param name="serializer"></param>
		/// <param name="heartbeatSettings"></param>
		/// <param name="latencySettings"></param>
		/// <param name="endPointSettings"></param>
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