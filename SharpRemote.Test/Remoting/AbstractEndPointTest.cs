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

		internal abstract IInternalRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, HeartbeatSettings heartbeatSettings = null);

		internal abstract IInternalRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, EndPointSettings endPointSettings = null, HeartbeatSettings heartbeatSettings = null);
	}
}