using System;
using System.Text;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.Test.Types.Interfaces.Web;
using SharpRemote.WebApi;
using SharpRemote.WebApi.Requests;

namespace SharpRemote.Test.WebApi.Requests
{
	[TestFixture]
	public sealed class RequestHandlerTest
	{
		public static readonly Uri Empty = new Uri("http://foo");

		[Test]
		public void TestGetString1()
		{
			var resource = new Mock<IGetString>();
			resource.Setup(x => x.Get()).Returns("Foobar");
			var handler = Resource.Create(resource.Object);
			var response = handler.TryHandleRequest("", new WebRequest(Empty, HttpMethod.Get));

			response.Should().NotBeNull();
			response.Code.Should().Be(200);
			response.Encoding.Should().Be(Encoding.UTF8);
			Encoding.UTF8.GetString(response.Content).Should().Be("\"Foobar\"");
			resource.Verify(x => x.Get(), Times.Once);
		}

		[Test]
		public void TestGetStringList1()
		{
			var resource = new Mock<IGetStringList>();
			resource.Setup(x => x.Get()).Returns(new[] {"Hello", "World!"});
			var handler = Resource.Create(resource.Object);
			var response = handler.TryHandleRequest("", new WebRequest(Empty, HttpMethod.Get));
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
			var response = handler.TryHandleRequest("9001", new WebRequest(Empty, HttpMethod.Get));
			response.Should().NotBeNull();
			response.Code.Should().Be(200);
			response.Encoding.Should().Be(Encoding.UTF8);
			Encoding.UTF8.GetString(response.Content).Should().Be("\"Sup\"");
		}
	}
}