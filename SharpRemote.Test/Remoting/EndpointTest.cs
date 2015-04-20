using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.CodeGeneration.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public sealed class EndpointTest
	{
		private PeerEndPoint _server;
		private PeerEndPoint _client;

		[SetUp]
		public void SetUp()
		{
			_server = new PeerEndPoint("Test", IPAddress.Loopback);
			_server.Start();

			_client = new PeerEndPoint("Test", IPAddress.Loopback);
			_client.Start();

			_client.Connect(_server.Address);
		}

		[Test]
		[Ignore("TBD")]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(42);

			var servant = _server.CreateServant(subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servant.ObjectId);
			proxy.Value.Should().Be(42);
		}
	}
}