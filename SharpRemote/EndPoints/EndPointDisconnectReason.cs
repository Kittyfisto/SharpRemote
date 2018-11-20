// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Describes the reason why a previously connected socket is now disconnected.
	/// </summary>
	public enum EndPointDisconnectReason
	{
		/// <summary>
		///     The connection was ended because <see cref="IRemotingEndPoint.Disconnect"/> was called.
		/// </summary>
		RequestedByEndPoint,

		/// <summary>
		///     The connection was ended because somebody called <see cref="IRemotingEndPoint.Disconnect"/> on
		///     the remote endpoint.
		/// </summary>
		RequestedByRemotEndPoint,

		/// <summary>
		///     The connection was dropped because a read operation on the socket failed.
		/// </summary>
		ReadFailure,

		/// <summary>
		///     The connection was dropped because a write operation on the socket failed.
		/// </summary>
		WriteFailure,

		/// <summary>
		///     The connection was dropped because a request with the same RPC id than an alrady
		///     pending request was made.
		/// </summary>
		RpcDuplicateRequest,

		/// <summary>
		///     The connection was dropped because a response to a non-existant pending RPC was received.
		/// </summary>
		RpcInvalidResponse,

		/// <summary>
		///     The connection was dropped because an unexpected exception.
		/// </summary>
		UnhandledException,

		/// <summary>
		///     The connection was dropped because the heartbeat monitor reported a failure.
		/// </summary>
		HeartbeatFailure,

		/// <summary>
		///     The connection was reset by the remote peer.
		/// </summary>
		/// <remarks>
		///     This error usually happens when the remote process exits unexpectedly without
		///     properly disconnecting first.
		/// </remarks>
		ConnectionReset,

		/// <summary>
		///     The connection was aborted by the underlying software on either this or the remote
		///     computer.
		/// </summary>
		ConnectionAborted,

		/// <summary>
		///     A read or write operation timed out. This usually means that the
		///     remote host can no longer be reached (for example a cable could have been disconnected
		///     or the peer computer could have powered down unexpectedly).
		/// </summary>
		ConnectionTimedOut,

		/// <summary>
		///     The reason for the disconnect is unknown.
		/// </summary>
		Unknown = int.MaxValue
	}
}