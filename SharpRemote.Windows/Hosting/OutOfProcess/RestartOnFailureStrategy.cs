using System;
using System.IO;
using System.Reflection;
using log4net;

namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// A <see cref="IFailureHandler"/> implementation that simply restarts the host process when failures occur.
	/// Both failures during start as well as failures during normal operation are expected, and if they happen, they
	/// are resolved by restarting the host process.
	/// </summary>
	/// <remarks>
	/// Tolerates a maximum of 10 successive Start() failures before giving up, unless the host process reported
	/// a <see cref="FileNotFoundException"/> in which case it gives up immediately.
	/// </remarks>
	/// <remarks>
	/// Tolerates an unlimited amount of failures during normal operations and simply restarts the host process if one occurs.
	/// </remarks>
	public sealed class RestartOnFailureStrategy
		: IFailureHandler
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly TimeSpan _baseWaitTime;
		private readonly int _startFailureThreshold;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="startFailureThreshold">The maximum amount of times the host process may fail starting until it is assumed to be broken and no more restart is tried</param>
		public RestartOnFailureStrategy(int startFailureThreshold = 10)
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
				Log.ErrorFormat("The host application failed to be started {0} times in a row - giving up", _startFailureThreshold);
				waitTime = TimeSpan.Zero;
				return Decision.Stop;
			}

			if (hostProcessException != null)
			{
				var fileNotFound = hostProcessException as FileNotFoundException;
				if (fileNotFound != null)
				{
					Log.ErrorFormat("The host application failed to start because '{0}' was not found - giving up: {1}",
					                fileNotFound.FileName,
					                fileNotFound);

					waitTime = TimeSpan.Zero;
					return Decision.Stop;
				}

				waitTime = TimeSpan.FromMilliseconds(numSuccessiveFailures*_baseWaitTime.TotalMilliseconds);
				Log.WarnFormat(
					"The host application failed to start because of an unexpected exception - trying again in {0} ms: {1}",
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