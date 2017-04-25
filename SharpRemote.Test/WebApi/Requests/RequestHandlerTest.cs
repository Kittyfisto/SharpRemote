using System;
using System.Text;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Interfaces.Web;
using SharpRemote.WebApi;
using SharpRemote.WebApi.Requests;
using SharpRemote.WebApi.Resources;

namespace SharpRemote.Test.WebApi.Requests
{
	[TestFixture]
	public sealed class RequestHandlerTest
	{
		public static readonly Uri Empty = new Uri("http://foo");
		public static readonly WebRequest EmptyGet = new WebRequest {Url = Empty, Method = HttpMethod.Get};
		public static readonly WebRequest EmptyPost = new WebRequest {Url =Empty, Method = HttpMethod.Post};

		[Test]
		public void TestGetString1()
		{
			var resource = new Mock<IGetString>();
			resource.Setup(x => x.Get()).Returns("Foobar");
			var handler = Resource.Create(resource.Object);
			var response = handler.TryHandleRequest("", EmptyGet);

			response.Should().NotBeNull();
			response.Code.Should().Be(200);
			response.Encoding.Should().Be(Encoding.UTF8);
			Encoding.UTF8.GetString(response.Content).Should().Be("\"Foobar\"");
			resource.Verify(x => x.Get(), Times.Once);
		}

		[Test]
		public void TestGetString2()
		{
			var resource = new Mock<IGetString>();
			resource.Setup(x => x.Get(It.IsAny<int>(), It.IsAny<int>()))
				.Returns((int startIndex, int count) => "Foobar".Substring(startIndex, count));
			var handler = Resource.Create(resource.Object);
			var response = handler.TryHandleRequest("startIndex=2&count=3", EmptyGet);
			response.Should().NotBeNull();
			response.Code.Should().Be(200);
			response.Encoding.Should().Be(Encoding.UTF8);
			Encoding.UTF8.GetString(response.Content).Should().Be("\"oba\"");
		}

		[Test]
		public void TestGetStringList1()
		{
			var resource = new Mock<IGetStringList>();
			resource.Setup(x => x.Get()).Returns(new[] {"Hello", "World!"});
			var handler = Resource.Create(resource.Object);
			var response = handler.TryHandleRequest("", EmptyGet);
			response.Should().NotBeNull();
			response.Code.Should().Be(200);
			response.Encoding.Should().Be(Encoding.UTF8);
			Encoding.UTF8.GetString(response.Content).Should().Be("[\"Hello\",\"World!\"]");
			resource.Verify(x => x.Get(), Times.Once);
		}

		[Test]
		public void TestGetStringList2()
		{
			var resource = new Mock<IGetStringList>();
			resource.Setup(x => x.Get(It.Is<int>(y => y == 9001))).Returns("Sup");
			var handler = Resource.Create(resource.Object);
			var response = handler.TryHandleRequest("9001", EmptyGet);
			response.Should().NotBeNull();
			response.Code.Should().Be(200);
			response.Encoding.Should().Be(Encoding.UTF8);
			Encoding.UTF8.GetString(response.Content).Should().Be("\"Sup\"");
		}

		[Test]
		public void TestGetStringList3()
		{
			var resource = new Mock<IGetStringList>();
			resource.Setup(x => x.Get(It.Is<int>(y => y == 9001))).Returns("Sup");
			var handler = Resource.Create(resource.Object);

			const string reason = "because the resource cannot handle the request";
			handler.TryHandleRequest("dwadawd", EmptyGet).Should().BeNull(reason);
			handler.TryHandleRequest("", EmptyPost).Should().BeNull(reason);
			handler.TryHandleRequest("id=9001", EmptyGet).Should().BeNull(reason);
			handler.TryHandleRequest("id=9001", EmptyPost).Should().BeNull(reason);
		}
	}
}