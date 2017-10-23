using System;
using System.Net;

namespace SharpRemote.Watchdog
{
	/// <summary>
	///     Responsible for hosting a <see cref="IInternalWatchdog" /> instance and exposing it via
	///     a <see cref="AbstractIPSocketRemotingEndPoint" />.
	/// </summary>
	public sealed class WatchdogHost
		: IDisposable
	{
		public const string PeerName = "SharpRemote.Watchdog";
		public const ulong ObjectId = 0;

		private readonly SocketRemotingEndPointServer _endPoint;
		private readonly InternalWatchdog _watchdog;

		/// <summary>
		/// Initializes this object.
		/// </summary>
		public WatchdogHost()
		{
			_watchdog = new InternalWatchdog();

			_endPoint = new SocketRemotingEndPointServer(PeerName);
			_endPoint.CreateServant(ObjectId, (IInternalWatchdog) _watchdog);
			_endPoint.Bind(IPAddress.Any);
		}

		/// <summary>
		/// 
		/// </summary>
		public EndPoint LocalEndPoint => _endPoint.LocalEndPoint;

		/// <inheritdoc />
		public void Dispose()
		{
			_watchdog.Dispose();
			_endPoint.Dispose();
		}
	}
}