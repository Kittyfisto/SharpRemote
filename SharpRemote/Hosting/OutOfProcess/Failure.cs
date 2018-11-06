namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// Describes the various reasons why the out of process silo faulted.
	/// </summary>
	public enum Failure
	{
		/// <summary>
		/// The configured amount of heartbeats were unanswered and thus the heartbeat monitor
		/// deemed the silo to have faulted.
		/// </summary>
		HeartbeatFailure,

		/// <summary>
		/// The connection between both endpoints failed unexpectedly.
		/// </summary>
		ConnectionFailure,

		/// <summary>
		/// The connection between both endpoints has been closed due to an unexpected exception
		/// on this endpoint.
		/// </summary>
		UnhandledException,

		/// <summary>
		/// The connection between both endpoints was closed intentionally.
		/// </summary>
		ConnectionClosed,

		/// <summary>
		/// The host process has exited for an unknown reason.
		/// One possible reason is that it crashed due to an unhandled exception.
		/// Another reason is that it might have been killed (either by a person or another program).
		/// </summary>
		HostProcessExited
	}
}