using System;
using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.CodeGeneration;
using SharpRemote.EndPoints;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.SystemTest.EndPoints
{
	[TestFixture]
	public sealed class ProxyStorageTest
	{
		private Mock<IRemotingEndPoint> _endPoint;
		private Mock<IEndPointChannel> _channel;
		private Mock<ICodeGenerator> _codeGenerator;

		[SetUp]
		public void Setup()
		{
			_endPoint = new Mock<IRemotingEndPoint>();
			_channel = new Mock<IEndPointChannel>();
			_codeGenerator = new Mock<ICodeGenerator>();
			_codeGenerator.Setup(x => x.CreateProxy<IByReferenceType>(It.IsAny<IRemotingEndPoint>(),
					It.IsAny<IEndPointChannel>(),
					It.IsAny<ulong>()))
				.Returns((IRemotingEndPoint endPoint, IEndPointChannel channel, ulong id) => CreateProxy<IByReferenceType>(endPoint, channel, id));
		}

		private T CreateProxy<T>(IRemotingEndPoint endPoint, IEndPointChannel channel, ulong objectId) where T : class
		{
			var obj = new Mock<T>();
			var proxy = obj.As<IProxy>();

			proxy.Setup(x => x.EndPoint).Returns(endPoint);
			proxy.Setup(x => x.InterfaceType).Returns(typeof(T));
			proxy.Setup(x => x.ObjectId).Returns(objectId);

			return obj.Object;
		}

		[Test]
		[Description("Verifies that it's possible to remove a singular proxy")]
		public void TestRemoveOneProxy()
		{
			var storage = new ProxyStorage(_endPoint.Object, _channel.Object, _codeGenerator.Object);

			var proxy = storage.CreateProxy<IByReferenceType>(1234);
			proxy.Should().NotBeNull();

			storage.GetProxy<IByReferenceType>(1234).Should().BeSameAs(proxy);
			storage.GetExistingOrCreateNewProxy<IByReferenceType>(1234).Should().BeSameAs(proxy);
			IProxy actualProxy;
			int unused;
			storage.TryGetProxy(1234, out actualProxy, out unused);
			actualProxy.Should().BeSameAs(proxy);

			storage.RemoveProxiesInRange(1234, 1234);
			new Action(() => storage.GetProxy<IByReferenceType>(1234)).ShouldThrow<ArgumentException>();
			var newProxy = storage.GetExistingOrCreateNewProxy<IByReferenceType>(1234);
			newProxy.Should().NotBeNull();
			newProxy.Should().NotBeSameAs(proxy);
			storage.TryGetProxy(1234, out actualProxy, out unused);
			actualProxy.Should().BeSameAs(newProxy);
		}

		[Test]
		[Description("Verifies that it's possible to remove only a particular range of proxies, while keeping others")]
		public void TestRemoveSeveralProxies()
		{
			var storage = new ProxyStorage(_endPoint.Object, _channel.Object, _codeGenerator.Object);

			var proxies = Enumerable.Range(1000, 100).Select(i => storage.CreateProxy<IByReferenceType>((ulong) i))
				.ToList();
			for (int i = 0; i < 100; ++i)
			{
				var objectId = (ulong)(1000 + i);
				storage.GetProxy<IByReferenceType>(objectId).Should().BeSameAs(proxies[i]);
			}

			storage.RemoveProxiesInRange(1042, 1058);

			for (int i = 0; i < 42; ++i)
			{
				var objectId = (ulong)(1000 + i);
				storage.GetProxy<IByReferenceType>(objectId).Should().BeSameAs(proxies[i]);
			}

			for (int i = 43; i < 59; ++i)
			{
				var objectId = (ulong) (1000 + i);
				new Action(() => storage.GetProxy<IByReferenceType>(objectId)).ShouldThrow<ArgumentException>();
				IProxy proxy;
				int numProxies;
				storage.TryGetProxy(objectId, out proxy, out numProxies);
				proxy.Should().BeNull();
			}

			for (int i = 59; i < 100; ++i)
			{
				var objectId = (ulong)(1000 + i);
				storage.GetProxy<IByReferenceType>(objectId).Should().BeSameAs(proxies[i]);
			}
		}
	}
}