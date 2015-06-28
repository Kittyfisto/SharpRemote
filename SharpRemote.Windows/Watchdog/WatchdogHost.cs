using System;
using System.Net;
using System.Net.PeerToPeer;

namespace SharpRemote.Watchdog
{
	/// <summary>
	///     Responsible for hosting a <see cref="IInternalWatchdog" /> instance and exposing it via
	///     a <see cref="SocketEndPoint" />.
	/// </summary>
	public sealed class WatchdogHost
		: IDisposable
	{
		public const string PeerName = "SharpRemote.Watchdog";
		public const ulong ObjectId = 0;

		private readonly SocketEndPoint _endPoint;
		private readonly PeerNameRegistration _peerNameRegistration;
		private readonly InternalWatchdog _watchdog;

		public WatchdogHost()
		{
			_watchdog = new InternalWatchdog();

			_endPoint = new SocketEndPoint(IPAddress.Any);
			_endPoint.CreateServant(ObjectId, (IInternalWatchdog) _watchdog);

			var peerName = new PeerName(PeerName, PeerNameType.Unsecured);
			_peerNameRegistration = new PeerNameRegistration
				{
					PeerName = peerName,
					Port = _endPoint.LocalEndPoint.Port,
					Comment = "1st version of the watchdog"
				};
			_peerNameRegistration.Start();
		}

		public EndPoint LocalEndPoint
		{
			get { return _endPoint.LocalEndPoint; }
		}

		public void Dispose()
		{
			_watchdog.Dispose();
			_endPoint.Dispose();
			_peerNameRegistration.Stop();
		}
	}
}