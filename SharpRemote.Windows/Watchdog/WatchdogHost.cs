using System;
using System.Net;

namespace SharpRemote.Watchdog
{
	/// <summary>
	///     Responsible for hosting a <see cref="IInternalWatchdog" /> instance and exposing it via
	///     a <see cref="SocketRemotingEndPoint" />.
	/// </summary>
	public sealed class WatchdogHost
		: IDisposable
	{
		public const string PeerName = "SharpRemote.Watchdog";
		public const ulong ObjectId = 0;

		private readonly SocketRemotingEndPointServer _endPoint;
		private readonly InternalWatchdog _watchdog;

		public WatchdogHost()
		{
			_watchdog = new InternalWatchdog();

			_endPoint = new SocketRemotingEndPointServer(PeerName);
			_endPoint.CreateServant(ObjectId, (IInternalWatchdog) _watchdog);
			_endPoint.Bind(IPAddress.Any);
		}

		public EndPoint LocalEndPoint
		{
			get { return _endPoint.LocalEndPoint; }
		}

		public void Dispose()
		{
			_watchdog.Dispose();
			_endPoint.Dispose();
		}
	}
}