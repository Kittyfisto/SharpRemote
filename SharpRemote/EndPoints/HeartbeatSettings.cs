using System;
using SharpRemote.Hosting;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Used to configure the heartbeat mechanism of <see cref="ISilo" />s.
	/// </summary>
	public sealed class HeartbeatSettings
	{
		/// <summary>
		///     NEVER, EVER USE THIS SETTING IN PRODUCTION CODE. NEVER!!!!
		/// </summary>
		public static HeartbeatSettings Dont => new HeartbeatSettings
		{
			AllowRemoteHeartbeatDisable = false,
			UseHeartbeatFailureDetection = false,
			ReportDebuggerAttached = false
		};

		/// <summary>
		///     Whether or not a remote endpoint can request to disable heartbeat detection on this
		///     endpoint.
		/// </summary>
		/// <remarks>
		///     This setting can be set to true to make interactive debugging easier: When an endpoint detects that
		///     a debugger is attached to its process, then it will request the other endpoint to disable its heartbeat
		///     detection UNTIL the debugger is detached again. This way a debugger can be attached to just one of the
		///     two processes AND set breakpoints, without having the endpoint of the other process perform a disconnection.
		/// </remarks>
		/// <remarks>
		///     Is set to false by default.
		/// </remarks>
		public bool AllowRemoteHeartbeatDisable;

		/// <summary>
		///     The minimum amount of time that shall pass between heartbeat checks.
		/// </summary>
		/// <remarks>
		///     Is set to 1 second by default.
		/// </remarks>
		public TimeSpan Interval;

		/// <summary>
		///     When set to true, the other endpoint is notified if a debugger is attached to this endpoint.
		///     When set to false, the other endpoint is never notified of this fact.
		/// </summary>
		/// <remarks>
		///     Is set to true by default.
		/// </remarks>
		public bool ReportDebuggerAttached;

		/// <summary>
		///     Whether or not skipped heartbeats are reported as failures when the debugger
		///     is attached on the monitoring process.
		/// </summary>
		/// <remarks>
		///     Having a debugger attached and actually pausing the monitoring process can cause
		///     a lot of heartbeats to be missed which is why this feature can be disabled for debugging sessions.
		/// </remarks>
		/// <remarks>
		///     Is set to false by default.
		/// </remarks>
		public bool ReportSkippedHeartbeatsAsFailureWithDebuggerAttached;

		/// <summary>
		///     The amount of heartbeats it takes for failure to be assumed.
		/// </summary>
		/// <remarks>
		///     Is set to 10 by default.
		/// </remarks>
		public int SkippedHeartbeatThreshold;

		/// <summary>
		///     Whether or not a "heartbeat" is used to regularly check the status of the host process.
		///     If the host process should fail to check in, then it is assumed to have faulted and is killed.
		/// </summary>
		/// <remarks>
		///     This value should always be set to true in production environments.
		/// </remarks>
		public bool UseHeartbeatFailureDetection;

		/// <summary>
		///     Initializes this class with its default values.
		/// </summary>
		public HeartbeatSettings()
		{
			ReportDebuggerAttached = true;
			ReportSkippedHeartbeatsAsFailureWithDebuggerAttached = false;
			AllowRemoteHeartbeatDisable = false;
			UseHeartbeatFailureDetection = true;
			Interval = TimeSpan.FromSeconds(1);
			SkippedHeartbeatThreshold = 10;
		}
	}
}