namespace SharpRemote.Hosting
{
	/// <summary>
	/// Describes the various reasons why a process faulted.
	/// </summary>
	public enum ProcessFailureReason
	{
		/// <summary>
		/// The connection between both endpoints has been closed due to an unexpected exception
		/// on this <see cref="ProcessWatchdog"/>.
		/// </summary>
		UnhandledException,

		/// <summary>
		/// The host process has exited.
		/// </summary>
		HostProcessExited,
	}
}
