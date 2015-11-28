namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// 
	/// </summary>
	public enum Decision
	{
		/// <summary>
		/// The host process shall be restarted in order to resolve the failure.
		/// </summary>
		RestartHost,

		/// <summary>
		/// The failure will be ignored, the host process, if still alive, be killed
		/// and from then on the <see cref="OutOfProcessSilo"/> will behave as having Stop()ed.
		/// </summary>
		Stop,
	}
}