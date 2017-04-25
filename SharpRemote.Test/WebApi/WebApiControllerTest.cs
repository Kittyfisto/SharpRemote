using System;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Extensions;
using SharpRemote.Test.Types.Interfaces.Web;
using SharpRemote.WebApi;
using SharpRemote.WebApi.HttpListener;

namespace SharpRemote.Test.WebApi
{
	[TestFixture]
	public sealed class WebApiControllerTest
	{
		private IPEndPoint _localEndPoint;
		private HttpClient _client;
		private HttpListener _listener;
		private string _apiUrl;
		private WebApiController _server;
		private SystemNetHttpListener _container;

		[SetUp]
		public void Setup()
		{
			_listener = new HttpListener();
			_localEndPoint = new IPEndPoint(IPAddress.Loopback, 8080);
			_apiUrl = string.Format("http://{0}/sharpremote/api/test/", _localEndPoint);
			_listener.Prefixes.Add(_apiUrl);
			_listener.Start();
			_client = new HttpClient();

			_server = new WebApiController();
			_container = new SystemNetHttpListener(_listener, _server);
		}

		[TearDown]
		public void TearDown()
		{
			_container?.Dispose();
			_server?.Dispose();
			_client?.Dispose();
			_listener?.TryDispose();
		}

		private Uri CreateUri(string suffix)
		{
			var uri = string.Format("{0}{1}", _apiUrl, suffix);
			return new Uri(uri, UriKind.Absolute);
		}

		[Test]
		public void TestGet1()
		{
			var controller = new Mock<IGameController>();
			_server.AddResource("games", controller.Object);

			var message = _client.Get(CreateUri("games"));
			message.StatusCode.Should().Be(HttpStatusCode.OK);
			message.GetContent().Should().Be("[]");
		}

		[Test]
		public void TestGet2()
		{
			var controller = new Mock<IGameController>();
			controller.Setup(x => x.Get(It.Is<int>(y => y == 42))).Returns(new Game(42, "Foo"));
			_server.AddResource("games", controller.Object);

			var message = _client.Get(CreateUri("games/42"));
			message.StatusCode.Should().Be(HttpStatusCode.OK);
			message.GetContent().Should().Be("{\"Id\":42,\"Name\":\"Foo\"}");
		}

		[Test]
		public void TestTwoIdenticalRoutesNotAllowed()
		{
			new Action(() => _server.AddResource("Test", new Mock<ITwoIdenticalRoutes>().Object))
				.ShouldThrow<ArgumentException>()
				.WithMessage("The method GetFoo() and GetBar() have the same route: This is not allowed");
		}
	}
}