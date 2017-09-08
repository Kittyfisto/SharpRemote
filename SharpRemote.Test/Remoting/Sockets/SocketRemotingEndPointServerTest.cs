using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class SocketRemotingEndPointServerTest
	{
		[Test]
		[Description("Verifies that any INetworkServiceDiscoverer implementation can be used")]
		public void TestBind1()
		{
			var discoverer = new Mock<INetworkServiceDiscoverer>();
			using (var server = new SocketRemotingEndPointServer(networkServiceDiscoverer: discoverer.Object,
			                                                     name: "foobar"))
			{
				server.Bind(IPAddress.Loopback);
				discoverer.Verify(x => x.RegisterService(It.Is<string>(name => name == "foobar"),
				                                         It.IsAny<IPEndPoint>(),
														 It.IsAny<byte[]>()),
				                  Times.Once);
			}
		}

		[Test]
		[LocalTest("Won't run on the server")]
		[Description("Verifies that if the same application already uses a given (addr, port) tuple on a non-exclusive port, then it won't be reported")]
		public void TestCreateSocketAndBindToAnyPort1()
		{
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				const ushort usedPort = 55555;
				socket.Bind(new IPEndPoint(IPAddress.Loopback, usedPort));

				IPEndPoint address;
				new Action(() =>
				           SocketRemotingEndPointServer.CreateSocketAndBindToAnyPort(IPAddress.Any, usedPort, usedPort,
				                                                                     out address)
					)
					.ShouldThrow<SystemException>()
					.WithMessage("No more available sockets");
			}
		}

		[Test]
		[LocalTest("Won't run on the server")]
		[Description("Verifies that if the same application already uses a given port, but on a different address (loopback vs. any), then this port won't be returned nevertheless")]
		public void TestCreateSocketAndBindToAnyPort2()
		{
			using (var socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				const ushort usedPort = 55555;
				socket.Bind(new IPEndPoint(IPAddress.Loopback, usedPort));

				IPEndPoint address;
				new Action(() =>
						   SocketRemotingEndPointServer.CreateSocketAndBindToAnyPort(IPAddress.Any, usedPort, usedPort,
																					 out address)
					)
					.ShouldThrow<SystemException>()
					.WithMessage("No more available sockets");
			}
		}

		[Test]
		[LocalTest("Won't run on the server")]
		[Description("Verifies that the created socket is set to exclusive mode")]
		public void TestCreateSocketAndBindToAnyPort3()
		{
			IPEndPoint address;
			using (var socket = SocketRemotingEndPointServer.CreateSocketAndBindToAnyPort(IPAddress.Any, out address))
			{
				socket.ExclusiveAddressUse.Should().BeTrue();
			}
		}
	}
}