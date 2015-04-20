using System;
using System.IO;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.Test.CodeGeneration.Types.Interfaces.PrimitiveTypes;

namespace SharpRemote.Test.CodeGeneration
{
	[TestFixture]
	[Description("Verifies that proxy and subject can communicate to each other- arguments, return values and exceptions are serialized already")]
	public sealed class CreatorTest
	{
		private IEndPointChannel _channel;
		private ProxyCreator _proxyCreator;
		private ServantCreator _servantCreator;
		private IServant _servant;
		private Random _random;

		private T CreateServantAndProxy<T>(T subject)
		{
			var objectId = (ulong) _random.Next();
			_servant = _servantCreator.CreateServant(objectId, subject);
			return _proxyCreator.CreateProxy<T>(objectId);
		}

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			var seed = (int) DateTime.Now.Ticks;
			_random = new Random(seed);

			var channel = new Mock<IEndPointChannel>();
			_channel = channel.Object;

			_proxyCreator = new ProxyCreator(_channel);
			_servantCreator = new ServantCreator();

			channel.Setup(x => x.CallRemoteMethod(It.IsAny<ulong>(), It.IsAny<string>(), It.IsAny<MemoryStream>()))
			       .Returns((ulong objectId, string methodName, Stream arguments) =>
				       {
					       if (objectId != _servant.ObjectId)
						       throw new NoSuchServantException(objectId);

					       BinaryReader reader = arguments != null ? new BinaryReader(arguments) : null;
					       var ret = new MemoryStream();
					       var writer = new BinaryWriter(ret);

					       _servant.Invoke(methodName, reader, writer);
					       ret.Position = 0;
					       return ret;
				       });
		}

		[Test]
		public void TestGetProperty()
		{
			var subject = new Mock<IGetDoubleProperty>();
			subject.Setup(x => x.Value).Returns(-414442.3213);

			var proxy = CreateServantAndProxy(subject.Object);
			proxy.Value.Should().BeApproximately(subject.Object.Value, 0);
		}
	}
}