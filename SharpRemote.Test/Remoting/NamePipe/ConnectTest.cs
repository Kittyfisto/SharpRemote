using System;
using System.Net;
using NUnit.Framework;
using SharpRemote.EndPoints;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.NamePipe
{
	[Ignore("Not yet working")]
	public sealed class ConnectTest
		: AbstractConnectTest
	{
		internal override IInternalRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new NamedPipeRemotingEndPointClient(name,
			                                           clientAuthenticator,
			                                           serverAuthenticator,
			                                           heartbeatSettings: heartbeatSettings,
			                                           latencySettings: latencySettings);
		}

		internal override IInternalRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null, IAuthenticator serverAuthenticator = null, LatencySettings latencySettings = null, EndPointSettings endPointSettings = null, HeartbeatSettings heartbeatSettings = null, NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new NamedPipeRemotingEndPointServer(name,
				clientAuthenticator,
				serverAuthenticator,
				heartbeatSettings: heartbeatSettings,
				latencySettings: latencySettings);
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((NamedPipeRemotingEndPointServer)endPoint).Bind();
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((NamedPipeRemotingEndPointServer)endPoint).Bind((NamedPipeEndPoint) address);
		}

		protected override EndPoint EndPoint1
		{
			get { return new NamedPipeEndPoint("a"); }
		}

		protected override EndPoint EndPoint2
		{
			get { return new NamedPipeEndPoint("b"); }
		}

		protected override EndPoint EndPoint3
		{
			get { return new NamedPipeEndPoint("c"); }
		}

		protected override EndPoint EndPoint4
		{
			get { return new NamedPipeEndPoint("d"); }
		}

		protected override EndPoint EndPoint5
		{
			get { return new NamedPipeEndPoint("e"); }
		}

		protected override ConnectionId Connect(IRemotingEndPoint endPoint, EndPoint address)
		{
			throw new NotImplementedException();
		}

		protected override void Connect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout)
		{
			((NamedPipeRemotingEndPointClient) endPoint).Connect((NamedPipeEndPoint) address, timeout);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name)
		{
			throw new NotImplementedException();
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name, TimeSpan timeout)
		{
			throw new NotImplementedException();
		}
	}
}