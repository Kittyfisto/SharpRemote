using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Remoting.Messaging;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;
using SharpRemote.Sockets;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class SocketEndPointServerTest
	{
		[Test]
		[Description("Verifies that any INetworkServiceDiscoverer implementation can be used")]
		public void TestBind1()
		{
			var discoverer = new Mock<INetworkServiceDiscoverer>();
			using (var server = new SocketEndPoint(EndPointType.Server,
			                                       networkServiceDiscoverer: discoverer.Object,
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
		[Ignore("Test not finished yet")]
		[Defect("https://github.com/Kittyfisto/SharpRemote/issues/41")]
		[Description("Verifies that the connection to an already successfully connected client is not disconnected just because EndConnect for a new client throws")]
		public void TestEndConnectException()
		{
			using (var server = new SocketEndPoint(EndPointType.Server,
			                                       heartbeatSettings: HeartbeatSettings.Dont))
			{
				var serverSocket = new Mock<ISocket>();
				var callbacks = new List<AsyncCallback>();
				var results = new List<IAsyncResult>();
				var sockets = new List<Mock<ISocket>>();
				serverSocket.Setup(x => x.BeginAccept(It.IsAny<AsyncCallback>(), It.IsAny<object>()))
				      .Returns((AsyncCallback cb, object state) =>
				      {
					      var result = new Mock<IAsyncResult>();
					      result.Setup(x => x.AsyncState).Returns(state);

						  callbacks.Add(cb);
						  results.Add(result.Object);

					      return result.Object;
				      });
				serverSocket.Setup(x => x.EndAccept(It.IsAny<IAsyncResult>())).Returns(() =>
				{
					var socket = new Mock<ISocket>();
					socket.Setup(x => x.Poll(It.IsAny<int>(), It.IsAny<SelectMode>()))
					                .Returns(true);
					socket.Setup(x => x.Connected).Returns(true);
					socket.Setup(x => x.Disconnect(It.IsAny<bool>()))
					      .Callback(() => socket.Setup(x => x.Connected).Returns(false));
					// TOOD: Setup methods so it appears a valid connection handshake is being performed
					sockets.Add(socket);
					return socket.Object;
				});
				
				server.Bind(serverSocket.Object);
				serverSocket.Verify(x => x.BeginAccept(It.IsAny<AsyncCallback>(), It.IsAny<object>()),
				              Times.Once);
				callbacks.Should().HaveCount(1);
				callbacks[0](results[0]);
				server.IsConnected.Should().BeTrue("because the server should've established a connection with our socket");

				serverSocket.Verify(x => x.BeginAccept(It.IsAny<AsyncCallback>(), It.IsAny<object>()),
				              Times.Exactly(2), "because the server should continue to listen to incoming connections");
				callbacks.Should().HaveCount(2);

				// This causes the actual failure: SocketEndPoint should not disconnect
				// the existing connection just because a new connection couldn't be established
				// after alll....
				serverSocket.Setup(x => x.EndAccept(It.IsAny<IAsyncResult>())).Throws<SocketException>();
				callbacks[1](results[1]);

				server.IsConnected.Should().BeTrue("because the connection to the first client should still be successful");
				const string reason = "because the already established connection should not be teared down just";
				sockets[0].Verify(x => x.Disconnect(It.IsAny<bool>()), Times.Never, reason);
				sockets[0].Verify(x => x.DisconnectAsync(It.IsAny<SocketAsyncEventArgs>()), Times.Never, reason);
				sockets[0].Verify(x => x.BeginDisconnect(It.IsAny<bool>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()), Times.Never, reason);
			}
		}

		[Test]
		[LocalTest("Won't run on the server")]
		[Description(
			"Verifies that if the same application already uses a given (addr, port) tuple on a non-exclusive port, then it won't be reported")]
		public void TestCreateSocketAndBindToAnyPort1()
		{
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				const ushort usedPort = 55555;
				socket.Bind(new IPEndPoint(IPAddress.Loopback, usedPort));

				IPEndPoint address;
				new Action(() =>
					           SocketEndPoint.CreateSocketAndBindToAnyPort(IPAddress.Any, usedPort, usedPort,
					                                                       out address)
				          )
					.ShouldThrow<SystemException>()
					.WithMessage("No more available sockets");
			}
		}

		[Test]
		[LocalTest("Won't run on the server")]
		[Description(
			"Verifies that if the same application already uses a given port, but on a different address (loopback vs. any), then this port won't be returned nevertheless")]
		public void TestCreateSocketAndBindToAnyPort2()
		{
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				const ushort usedPort = 55555;
				socket.Bind(new IPEndPoint(IPAddress.Loopback, usedPort));

				IPEndPoint address;
				new Action(() =>
					           SocketEndPoint.CreateSocketAndBindToAnyPort(IPAddress.Any, usedPort, usedPort,
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
			using (var socket = SocketEndPoint.CreateSocketAndBindToAnyPort(IPAddress.Any, out address))
			{
				socket.ExclusiveAddressUse.Should().BeTrue();
			}
		}
	}
}