namespace SharpRemote.Hosting
{
	/// <summary>
	/// Defines whether or not a console window for the host process should be shown, or not.
	/// </summary>
	public enum ProcessOptions
	{
		/// <summary>
		/// A console shall be shown.
		/// </summary>
		ShowConsole,

		/// <summary>
		/// No console shall be shown - the host process is invisible to a user (besides
		/// inspection of the process list).
		/// </summary>
		HideConsole,
	}
}