using System;
using System.Collections.Generic;
using System.Net;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public class AcceptanceTest
		: AbstractAcceptanceTest
	{
		protected override void Connect(IRemotingEndPoint client, IRemotingEndPoint server)
		{
			((SocketEndPoint)client).Connect(
				((ISocketEndPoint)server).LocalEndPoint,
				TimeSpan.FromMinutes(1));
		}

		protected override IEnumerable<IServant> Servants(IRemotingEndPoint client)
		{
			return ((SocketEndPoint) client).Servants;
		}

		protected override IRemotingEndPoint CreateClient()
		{
			return new SocketEndPoint(EndPointType.Client, "Client");
		}

		protected override IRemotingEndPoint CreateServer()
		{
			return new SocketEndPoint(EndPointType.Server, "Server");
		}

		protected override void Bind(IRemotingEndPoint server)
		{
			((ISocketEndPoint)server).Bind(IPAddress.Loopback);
		}
	}
}