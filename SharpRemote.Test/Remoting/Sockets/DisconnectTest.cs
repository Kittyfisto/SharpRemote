using System;
using System.Net;
using NUnit.Framework;
using SharpRemote.EndPoints;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class DisconnectTest
		: AbstractDisconnectTest
	{
		internal override IInternalRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointClient(name, clientAuthenticator, serverAuthenticator, null,
													latencySettings: latencySettings,
													heartbeatSettings: heartbeatSettings);
		}

		internal override IInternalRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, EndPointSettings endPointSettings = null, HeartbeatSettings heartbeatSettings = null)
		{
			return new SocketRemotingEndPointServer(name,
													clientAuthenticator,
													serverAuthenticator, null,
													latencySettings: latencySettings,
													endPointSettings: endPointSettings,
													heartbeatSettings: heartbeatSettings);
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((SocketRemotingEndPointServer)endPoint).Bind(IPAddress.Loopback);
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((SocketRemotingEndPointServer)endPoint).Bind((IPEndPoint) address);
		}

		protected override void Connect(IRemotingEndPoint client, EndPoint localEndPoint)
		{
			((SocketRemotingEndPointClient) client).Connect((IPEndPoint) localEndPoint);
		}

		protected override void Connect(IRemotingEndPoint client, EndPoint localEndPoint, TimeSpan timeout)
		{
			((SocketRemotingEndPointClient) client).Connect((IPEndPoint) localEndPoint, timeout);
		}

		protected override bool TryConnect(IRemotingEndPoint client, EndPoint localEndPoint, TimeSpan timeout)
		{
			return ((SocketRemotingEndPointClient) client).TryConnect((IPEndPoint) localEndPoint, timeout);
		}
	}
}