using System;
using System.IO;
using System.Reflection;
using log4net;

namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// The default <see cref="IFailureHandler"/> implementation that controls how host-process failures are handled.
	/// </summary>
	/// <remarks>
	/// Tollerates a maximum of 10 successive Start() failures before giving up, unless the host process reported
	/// a <see cref="FileNotFoundException"/> in which case it gives up immediately.
	/// </remarks>
	/// <remarks>
	/// Every failure after a successful start results in the host process being restarted.
	/// </remarks>
	public sealed class DefaultFailureHandler
		: IFailureHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly TimeSpan _baseWaitTime;
		private readonly int _startFailureThreshold;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="startFailureThreshold">The maximum amount of times the host process may fail starting until it is assumed to be broken and no more restart is tried</param>
		public DefaultFailureHandler(int startFailureThreshold = 10)
		{
			if (startFailureThreshold < 0)
				throw new ArgumentOutOfRangeException("startFailureThreshold");

			_baseWaitTime = TimeSpan.FromMilliseconds(10);
			_startFailureThreshold = startFailureThreshold;
		}

		public Decision? OnStartFailure(int numSuccessiveFailures, Exception hostProcessException, out TimeSpan waitTime)
		{
			if (numSuccessiveFailures > _startFailureThreshold)
			{
				Log.ErrorFormat("The host process failed to be started {0} times in a row - giving up", _startFailureThreshold);
				waitTime = TimeSpan.Zero;
				return Decision.Stop;
			}

			if (hostProcessException != null)
			{
				var fileNotFound = hostProcessException as FileNotFoundException;
				if (fileNotFound != null)
				{
					Log.ErrorFormat("The host process failed to start because '{0}' was not found - giving up: {1}",
					                fileNotFound.FileName,
					                fileNotFound);

					waitTime = TimeSpan.Zero;
					return Decision.Stop;
				}

				waitTime = TimeSpan.FromMilliseconds(numSuccessiveFailures*_baseWaitTime.TotalMilliseconds);
				Log.WarnFormat(
					"The host process failed to start because if caught an unexpected exception - trying again in {0} ms: {1}",
					waitTime,
					hostProcessException);
				return Decision.RestartHost;
			}

			// Timeout / connection problem - let's try again and it'll probably be resolved.
			waitTime = TimeSpan.FromMilliseconds(numSuccessiveFailures * _baseWaitTime.TotalMilliseconds);
			return Decision.RestartHost;
		}

		public Decision? OnFailure(Failure failure)
		{
			switch (failure)
			{
				case Failure.ConnectionFailure:
				case Failure.HeartbeatFailure:
				case Failure.HostProcessExited:
				case Failure.UnhandledException:
				case Failure.ConnectionClosed:
					return Decision.RestartHost;

				default:
					Log.WarnFormat("Unknown failure '{0}' - restarting host", failure);
					return Decision.RestartHost;
			}
		}

		public void OnResolutionFailed(Failure failure, Decision decision, Exception exception)
		{
			
		}

		public void OnResolutionFinished(Failure failure, Decision decision, Resolution resolution)
		{
			
		}
	}
}