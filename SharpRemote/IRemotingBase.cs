using System;
using System.Net;

namespace SharpRemote
{
	/// <summary>
	///     The most basic interface which collects the commonality between
	///     <see cref="IRemotingServer" /> and <see cref="IRemotingEndPoint" />.
	/// </summary>
	public interface IRemotingBase
		: IDisposable
	{
		/// <summary>
		///     The name of this endpoint, only used for debugging.
		/// </summary>
		string Name { get; }

		/// <summary>
		///     The endpoint this object is bound to.
		/// </summary>
		EndPoint LocalEndPoint { get; }

		#region Statistics

		/// <summary>
		///     The total amount of bytes that have been sent over the underlying stream.
		/// </summary>
		long NumBytesSent { get; }

		/// <summary>
		///     The total amount of bytes that have been received over the underlying stream.
		/// </summary>
		long NumBytesReceived { get; }

		/// <summary>
		///     The total amount of messages that have been sent over the underlying stream.
		/// </summary>
		long NumMessagesSent { get; }

		/// <summary>
		///     The total amount of messages that have been received over the underlying stream.
		/// </summary>
		long NumMessagesReceived { get; }

		/// <summary>
		///     The total amount of remote procedure calls that have been invoked from this end.
		/// </summary>
		long NumCallsInvoked { get; }

		/// <summary>
		///     The total amount of remote procedure calls that have been invoked from the other end.
		/// </summary>
		long NumCallsAnswered { get; }

		/// <summary>
		///     The current number of method calls which have been invoked, but have not been sent over
		///     the underlying stream.
		/// </summary>
		long NumPendingMethodCalls { get; }
		
		/// <summary>
		///     The total number of method invocations that have been retrieved from the underlying stream,
		///     but not yet invoked or not yet finished.
		/// </summary>
		long NumPendingMethodInvocations { get; }

		/// <summary>
		///     The average roundtrip time of messages.
		/// </summary>
		/// <remarks>
		///     Set to <see cref="TimeSpan.Zero" /> in case latency measurements are disabled.
		/// </remarks>
		TimeSpan? AverageRoundTripTime { get; }

		/// <summary>
		///     The total amount of time this endpoint spent collecting garbage.
		/// </summary>
		TimeSpan TotalGarbageCollectionTime { get; }

		#endregion

		#region Settings

		/// <summary>
		///     The settings used for the endpoint itself (max. number of concurrent calls, etc...).
		/// </summary>
		/// <remarks>
		///     Changing these after construction has no effect.
		/// </remarks>
		EndPointSettings EndPointSettings { get; }

		/// <summary>
		///     The settings used for latency measurements.
		/// </summary>
		/// <remarks>
		///     Changing these after construction has no effect.
		/// </remarks>
		LatencySettings LatencySettings { get; }

		/// <summary>
		///     The settings used for the heartbeat mechanism.
		/// </summary>
		/// <remarks>
		///     Changing these after construction has no effect.
		/// </remarks>
		HeartbeatSettings HeartbeatSettings { get; }

		#endregion
	}
}