using System;

namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// Used to configure how (or if at all) an <see cref="OutOfProcessSilo"/> assumes that an error occured.
	/// </summary>
	public sealed class FailureSettings
	{
		/// <summary>
		/// The maximum amount of time that may pass between starting the host process and it
		/// signalling to be ready to accept incoming connections. After this time has passed
		/// the host process is assumed to have faulted and is killed. A <see cref="IFailureHandler"/>
		/// can then control when, or if at all, the host process is started again.
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
		/// establishes a connection with the host process before the process is assumed to have failed
		/// and is killed.
		/// </summary>
		public TimeSpan EndPointConnectTimeout;

		/// <summary>
		/// The settings specifying how (or if at all) the heartbeat mechanism is employed
		/// to detect (assumed) failures in the host process.
		/// </summary>
		/// <remarks>
		/// By default a heartbeat mechanism is employed - if the host process doesn't respond
		/// within 10 seconds then it's assumed to have failed and is killed. A <see cref="IFailureHandler"/>
		/// is invoked to determine what happens next.
		/// </remarks>
		public HeartbeatSettings HeartbeatSettings;

		/// <summary>
		/// 
		/// </summary>
		public FailureSettings()
		{
			ProcessReadyTimeout = TimeSpan.FromSeconds(10);
			EndPointConnectTimeout = TimeSpan.FromSeconds(2);
			HeartbeatSettings = new HeartbeatSettings();
		}
	}
}