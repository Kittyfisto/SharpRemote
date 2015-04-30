using System;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.CodeGeneration.Types.Exceptions;
using SharpRemote.Test.CodeGeneration.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.Remoting
{
	[TestFixture]
	public sealed class EndpointAcceptanceTest
	{
		private PeerEndPoint _server;
		private PeerEndPoint _client;

		[TestFixtureSetUp]
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

		[Test]
		[Description("Verifies that an eception can be marshalled")]
		public void TestThrowException1()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(() =>
				{
					throw new ArgumentException("Foobar");
				});

			const int servantId = 2;
			var servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			new Action(() => { var unused = proxy.Value; })
				.ShouldThrow<ArgumentException>()
				.WithMessage("Foobar");
		}

		[Test]
		[Description("Verifies that if an exception could not be serialized, but can be re-constructed due to a default ctor, then it is thrown again")]
		public void TestThrowNonSerializableException()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(() =>
			{
				throw new NonSerializableExceptionButDefaultCtor();
			});

			const int servantId = 3;
			var servant = _server.CreateServant(servantId, subject.Object);
			var proxy = _client.CreateProxy<IGetDoubleProperty>(servantId);
			new Action(() => { var unused = proxy.Value; })
				.ShouldThrow<UnserializableException>();
		}
	}
}