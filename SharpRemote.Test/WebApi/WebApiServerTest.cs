using System.Net;
using NUnit.Framework;
using SharpRemote.WebApi;
using SharpRemote.WebApi.HttpListener;

namespace SharpRemote.Test.WebApi
{
	[TestFixture]
	public sealed class WebApiServerTest
	{
		private readonly IPEndPoint _localEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);

		[Test]
		[Ignore("not yet implemented")]
		public void TestRegisterResource1()
		{
			using (var server = new WebApiController())
			using (var listener = new SystemNetHttpListener(server))
			{
				server.AddResource("Games", new GameController());
			}
		}
	}
}