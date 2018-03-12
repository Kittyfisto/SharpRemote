using System;
using System.Net;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class DisconnectTest
		: AbstractDisconnectTest
	{
		internal override IRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketEndPoint(EndPointType.Client,
			                          name, clientAuthenticator, serverAuthenticator, null,
													latencySettings: latencySettings,
													heartbeatSettings: heartbeatSettings);
		}

		internal override IRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, EndPointSettings endPointSettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new SocketEndPoint(EndPointType.Server,
			                          name,
													clientAuthenticator,
													serverAuthenticator, null,
													latencySettings: latencySettings,
													endPointSettings: endPointSettings,
													heartbeatSettings: heartbeatSettings);
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((ISocketEndPoint)endPoint).Bind(IPAddress.Loopback);
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((ISocketEndPoint)endPoint).Bind((IPEndPoint) address);
		}

		protected override void Connect(IRemotingEndPoint client, EndPoint localEndPoint)
		{
			((SocketEndPoint) client).Connect((IPEndPoint) localEndPoint);
		}

		protected override void Connect(IRemotingEndPoint client, EndPoint localEndPoint, TimeSpan timeout)
		{
			((SocketEndPoint) client).Connect((IPEndPoint) localEndPoint, timeout);
		}

		protected override bool TryConnect(IRemotingEndPoint client, EndPoint localEndPoint, TimeSpan timeout)
		{
			return ((SocketEndPoint) client).TryConnect((IPEndPoint) localEndPoint, timeout);
		}
	}
}