using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting.NamePipe
{
	[TestFixture]
	[Ignore("Not yet working")]
	public class AcceptanceTest
		: AbstractAcceptanceTest
	{
		protected override void Connect(IRemotingEndPoint client, IRemotingEndPoint server)
		{
			((NamedPipeRemotingEndPointClient)client).Connect(
				((NamedPipeRemotingEndPointServer)server).LocalEndPoint,
				TimeSpan.FromMinutes(1));
		}

		protected override IEnumerable<IServant> Servants(IRemotingEndPoint client)
		{
			return ((NamedPipeRemotingEndPointClient)client).Servants;
		}

		protected override IRemotingEndPoint CreateClient()
		{
			return new NamedPipeRemotingEndPointClient("Client");
		}

		protected override IRemotingEndPoint CreateServer()
		{
			return new NamedPipeRemotingEndPointServer("Server");
		}

		protected override void Bind(IRemotingEndPoint server)
		{
			((NamedPipeRemotingEndPointServer)server).Bind();
		}
	}
}