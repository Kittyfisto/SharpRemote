using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	[TestFixture]
	public sealed class ConnectTest
		: AbstractConnectTest
	{
		public override LogItem[] Loggers
		{
			get
			{
				return new[]
					{
						new LogItem(typeof (SocketRemotingEndPointClient)),
						new LogItem(typeof (SocketRemotingEndPointServer))
					};
			}
		}

		[Test]
		[LocalTest("I swear to god, you cannot run any fucking test on this CI server")]
		[Description("Verifies that Connect() can establish a connection with an endpoint by specifying its name")]
		public void TestConnect2()
		{
			using (var discoverer = new NetworkServiceDiscoverer())
			using (var client = CreateClient("Rep1", networkServiceDiscoverer: discoverer))
			using (var server = CreateServer("Rep2", networkServiceDiscoverer: discoverer))
			{
				Bind(server);

				client.IsConnected.Should().BeFalse();
				client.RemoteEndPoint.Should().BeNull();

				server.IsConnected.Should().BeFalse();
				server.RemoteEndPoint.Should().BeNull();

				// ReSharper disable AccessToDisposedClosure
				new Action(() => Connect(client, server.Name, TimeSpan.FromSeconds(10)))
					// ReSharper restore AccessToDisposedClosure
					.ShouldNotThrow();

				client.IsConnected.Should().BeTrue();
				client.RemoteEndPoint.Should().Be(server.LocalEndPoint);

				server.IsConnected.Should().BeTrue();
			}
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((SocketRemotingEndPointServer)endPoint).Bind(IPAddress.Loopback);
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((SocketRemotingEndPointServer)endPoint).Bind((IPEndPoint) address);
		}

		protected override EndPoint EndPoint1
		{
			get { return new IPEndPoint(IPAddress.Loopback, 50012); }
		}

		protected override EndPoint EndPoint2
		{
			get { return new IPEndPoint(IPAddress.Loopback, 12345); }
		}

		protected override EndPoint EndPoint3
		{
			get { return new IPEndPoint(IPAddress.Loopback, 54321); }
		}

		protected override EndPoint EndPoint4
		{
			get { return new IPEndPoint(IPAddress.Loopback, 58752); }
		}

		protected override EndPoint EndPoint5
		{
			get { return new IPEndPoint(IPAddress.Loopback, 1234); }
		}

		protected override ConnectionId Connect(IRemotingEndPoint endPoint, EndPoint address)
		{
			return ((SocketRemotingEndPointClient) endPoint).Connect((IPEndPoint) address);
		}

		protected override void Connect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout)
		{
			((SocketRemotingEndPointClient) endPoint).Connect((IPEndPoint) address, timeout);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name)
		{
			((SocketRemotingEndPointClient) endPoint).Connect(name);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name, TimeSpan timeout)
		{
			((SocketRemotingEndPointClient)endPoint).Connect(name, timeout);
		}
	}
}
