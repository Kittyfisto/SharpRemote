namespace SharpRemote.Hosting
{
	/// <summary>
	/// Describes the various states of how (and if) a fault has been resolved.
	/// </summary>
	public enum OutOfProcessSiloFaultHandling
	{
		/// <summary>
		/// The problem has been resolved but failed calls on remote objects may or may not have been
		/// completely executed.
		/// </summary>
		Resolved,

		/// <summary>
		/// The problem could not be resolved, but the host process was restarted and is now in its
		/// initial state.
		/// </summary>
		Restarted,

		/// <summary>
		/// The problem could not be resolved and restarting the host process failed or was not allowed.
		/// The silo can no longer be used.
		/// </summary>
		Shutdown,
	}
}