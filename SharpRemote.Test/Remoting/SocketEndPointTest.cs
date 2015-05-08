using System.Net;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public sealed class SocketEndPointTest
		: EndPointTest
	{
		protected override IRemotingEndPoint CreateEndPoint(IPAddress address, string name = null)
		{
			return new SocketEndPoint(address, name);
		}
	}
}