using System.Net;
using SharpRemote.EndPoints;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting
{
	public abstract class AbstractEndPointTest
		: AbstractTest
	{
		protected abstract void Bind(IRemotingEndPoint endPoint);
		protected abstract void Bind(IRemotingEndPoint endPoint, EndPoint address);

		internal IInternalRemotingEndPoint CreateClient(string name = null,
		                                         IAuthenticator clientAuthenticator = null,
		                                         IAuthenticator serverAuthenticator = null,
		                                         NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                         LatencySettings latencySettings = null,
		                                         HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointClient(name, clientAuthenticator, serverAuthenticator, null,
			                                        networkServiceDiscoverer,
			                                        latencySettings: latencySettings,
			                                        heartbeatSettings: heartbeatSettings);
		}

		internal IInternalRemotingEndPoint CreateServer(string name = null,
		                                         IAuthenticator clientAuthenticator = null,
		                                         IAuthenticator serverAuthenticator = null,
		                                         NetworkServiceDiscoverer networkServiceDiscoverer = null,
		                                         LatencySettings latencySettings = null,
		                                         EndPointSettings endPointSettings = null,
		                                         HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointServer(name,
			                                        clientAuthenticator,
			                                        serverAuthenticator, null,
			                                        networkServiceDiscoverer,
			                                        latencySettings: latencySettings,
			                                        endPointSettings: endPointSettings,
			                                        heartbeatSettings: heartbeatSettings);
		}
	}
}