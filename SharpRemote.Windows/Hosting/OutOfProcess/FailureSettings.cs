using System;

namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class FailureSettings
	{
		/// <summary>
		/// The maximum amount of time that may pass between starting the process and it
		/// communicating its used port number back to the <see cref="OutOfProcessSilo"/>,
		/// before the process is assumed to have failed and is killed.
		/// </summary>
		/// <remarks>
		/// Is set to 10 seconds by default.
		/// </remarks>
		/// <remarks>
		/// If your (custom) application requires a lot of startup time then you can change this setting to a higher
		/// value to tolerate this.
		/// </remarks>
		public TimeSpan ProcessReadyTimeout;

		/// <summary>
		/// The maximum amount of time that may pass until the <see cref="OutOfProcessSilo"/>
		/// establishes a connection with the child process before the process is assumed to have failed
		/// and is killed.
		/// </summary>
		public TimeSpan EndPointConnectTimeout;

		/// <summary>
		/// 
		/// </summary>
		public FailureSettings()
		{
			ProcessReadyTimeout = TimeSpan.FromSeconds(10);
			EndPointConnectTimeout = TimeSpan.FromSeconds(2);
		}
	}
}