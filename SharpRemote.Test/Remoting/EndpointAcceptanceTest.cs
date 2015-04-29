using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.CodeGeneration.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public sealed class EndpointAcceptanceTest
	{
		private PeerEndPoint _server;
		private PeerEndPoint _client;

		[SetUp]
		public void SetUp()
		{
			_server = new PeerEndPoint("Server", IPAddress.Loopback);
			_server.Start();

			_client = new PeerEndPoint("Client", IPAddress.Loopback);
			_client.Start();

			_client.Connect(_server.Address);
		}

		[Test]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(42);

			const int servantId = 1;
			var servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			proxy.Value.Should().Be(42);
		}
	}
}