namespace SharpRemote.Hosting
{
	/// <summary>
	///     Defines the various states of the host process.
	/// </summary>
	public enum HostState
	{
		/// <summary>
		///     Default state, should not appear in the wild.
		/// </summary>
		None,

		/// <summary>
		///     The host will boot in the near future.
		/// </summary>
		BootPending,

		/// <summary>
		///     The host is booting up, but hasn't finished yet.
		/// </summary>
		Booting,

		/// <summary>
		///     The host has booted up and is ready to be used.
		/// </summary>
		Ready,

		/// <summary>
		///     The host is shutting down.
		/// </summary>
		ShuttingDown,

		/// <summary>
		///     The host process is no longer running.
		/// </summary>
		Dead
	}
}