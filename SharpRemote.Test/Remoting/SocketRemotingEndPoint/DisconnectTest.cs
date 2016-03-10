using System;
using System.Net;
using NUnit.Framework;

namespace SharpRemote.Test.Remoting.SocketRemotingEndPoint
{
	[TestFixture]
	public sealed class DisconnectTest
		: AbstractDisconnectTest
	{
		protected override void Bind(IRemotingEndPoint endPoint)
		{
			((SocketRemotingEndPointServer)endPoint).Bind(IPAddress.Loopback);
		}

		protected override void Bind(IRemotingEndPoint endPoint, EndPoint address)
		{
			((SocketRemotingEndPointServer)endPoint).Bind((IPEndPoint) address);
		}

		protected override void Connect(IRemotingEndPoint client, EndPoint localEndPoint)
		{
			((SocketRemotingEndPointClient) client).Connect((IPEndPoint) localEndPoint);
		}

		protected override void Connect(IRemotingEndPoint client, EndPoint localEndPoint, TimeSpan timeout)
		{
			((SocketRemotingEndPointClient) client).Connect((IPEndPoint) localEndPoint, timeout);
		}

		protected override bool TryConnect(IRemotingEndPoint client, EndPoint localEndPoint, TimeSpan timeout)
		{
			return ((SocketRemotingEndPointClient) client).TryConnect((IPEndPoint) localEndPoint, timeout);
		}
	}
}