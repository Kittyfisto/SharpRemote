using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using SharpRemote.ServiceDiscovery;

namespace SharpRemote.Test.Remoting.NamePipe
{
	[TestFixture]
	[Ignore("Not yet finished")]
	public sealed class ConnectTest
		: AbstractConnectTest
	{
		internal override IRemotingEndPoint CreateClient(string name = null, IAuthenticator clientAuthenticator = null,
		                                                 IAuthenticator serverAuthenticator = null,
		                                                 LatencySettings latencySettings = null,
		                                                 HeartbeatSettings heartbeatSettings = null,
		                                                 NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new NamedPipeRemotingEndPointClient(name,
			                                           clientAuthenticator,
			                                           serverAuthenticator,
			                                           heartbeatSettings: heartbeatSettings,
			                                           latencySettings: latencySettings);
		}

		internal override IRemotingEndPoint CreateServer(string name = null, IAuthenticator clientAuthenticator = null,
		                                                 IAuthenticator serverAuthenticator = null,
		                                                 LatencySettings latencySettings = null,
		                                                 EndPointSettings endPointSettings = null,
		                                                 HeartbeatSettings heartbeatSettings = null,
		                                                 NetworkServiceDiscoverer networkServiceDiscoverer = null)
		{
			return new NamedPipeRemotingEndPointServer(name,
			                                           clientAuthenticator,
			                                           serverAuthenticator,
			                                           heartbeatSettings: heartbeatSettings,
			                                           latencySettings: latencySettings);
		}

		[Test]
		[Description(
			"Verifies that Connect() cannot establish a connection with a non-existant endpoint and returns in the specified timeout"
			)]
		public void TestConnect3()
		{
			using (var rep = CreateClient())
			{
				TimeSpan timeout = TimeSpan.FromMilliseconds(100);
				new Action(
					() => new Action(() => Connect(rep, EndPoint1, timeout))
							  .ShouldThrow<NoSuchNamedPipeEndPointException>()
							  .WithMessage("Unable to establish a connection with the given endpoint after 100 ms: a (Server)"))
					.ExecutionTime().ShouldNotExceed(TimeSpan.FromSeconds(2));

				const string reason = "because no successfull connection could be established";
				rep.IsConnected.Should().BeFalse(reason);
				rep.RemoteEndPoint.Should().BeNull(reason);
			}
		}

		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((NamedPipeRemotingEndPointServer) endPoint).Bind();
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((NamedPipeRemotingEndPointServer) endPoint).Bind((NamedPipeEndPoint) address);
		}

		protected override EndPoint EndPoint1
		{
			get { return new NamedPipeEndPoint("a", NamedPipeEndPoint.PipeType.Server); }
		}

		protected override EndPoint EndPoint2
		{
			get { return new NamedPipeEndPoint("b", NamedPipeEndPoint.PipeType.Server); }
		}

		protected override EndPoint EndPoint3
		{
			get { return new NamedPipeEndPoint("c", NamedPipeEndPoint.PipeType.Server); }
		}

		protected override EndPoint EndPoint4
		{
			get { return new NamedPipeEndPoint("d", NamedPipeEndPoint.PipeType.Server); }
		}

		protected override EndPoint EndPoint5
		{
			get { return new NamedPipeEndPoint("e", NamedPipeEndPoint.PipeType.Server); }
		}

		protected override ConnectionId Connect(IRemotingEndPoint endPoint, EndPoint address)
		{
			return ((NamedPipeRemotingEndPointClient)endPoint).Connect((NamedPipeEndPoint) address);
		}

		protected override void Connect(IRemotingEndPoint endPoint, EndPoint address, TimeSpan timeout)
		{
			((NamedPipeRemotingEndPointClient) endPoint).Connect((NamedPipeEndPoint) address, timeout);
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name)
		{
			throw new NotImplementedException();
		}

		protected override void Connect(IRemotingEndPoint endPoint, string name, TimeSpan timeout)
		{
			throw new NotImplementedException();
		}
	}
}