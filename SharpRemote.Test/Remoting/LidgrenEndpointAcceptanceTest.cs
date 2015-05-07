using System.Net;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public sealed class LidgrenEndpointAcceptanceTest
		: EndPointAcceptanceTest
	{
		protected override IRemotingEndPoint CreateEndPoint(IPAddress address, string name = null)
		{
			return new LidgrenEndPoint(address, name);
		}
	}
}