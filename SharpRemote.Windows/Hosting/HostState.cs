namespace SharpRemote.Hosting
{
	/// <summary>
	/// Defines the various states of the host process.
	/// </summary>
	public enum HostState
	{
		None,

		BootPending,
		Booting,
		Ready,
		ShuttingDown,
	}
}