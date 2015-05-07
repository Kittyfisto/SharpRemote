using System.Net;
using NUnit.Framework;
using SharpRemote.EndPoints;

namespace SharpRemote.Test.Remoting
{
	[Ignore]
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