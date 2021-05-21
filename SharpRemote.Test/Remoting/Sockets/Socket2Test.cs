using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.Sockets;

namespace SharpRemote.Test.Remoting.Sockets
{
	[TestFixture]
	public sealed class Socket2Test
	{
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
					           Socket2.CreateSocketAndBindToAnyPort(IPAddress.Any, usedPort, usedPort,
					                                                out address)
				          )
					.Should().Throw<SystemException>()
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
					           Socket2.CreateSocketAndBindToAnyPort(IPAddress.Any, usedPort, usedPort,
					                                                out address)
				          )
					.Should().Throw<SystemException>()
					.WithMessage("No more available sockets");
			}
		}

		[Test]
		[LocalTest("Won't run on the server")]
		[Description("Verifies that the created socket is set to exclusive mode")]
		public void TestCreateSocketAndBindToAnyPort3()
		{
			IPEndPoint address;
			using (var socket = Socket2.CreateSocketAndBindToAnyPort(IPAddress.Any, out address))
			{
				socket.ExclusiveAddressUse.Should().BeTrue();
			}
		}
	}
}