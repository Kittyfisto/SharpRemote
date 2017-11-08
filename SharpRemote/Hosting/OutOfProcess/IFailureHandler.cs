using System;

namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// This interface can be used to control how, if and when failures of the host process or its connection
	/// to this one is handled.
	/// </summary>
	public interface IFailureHandler
	{
		/// <summary>
		/// This method is called when the host process failed to start, did not report to be ready or no connection
		/// could be established with the host process.
		/// </summary>
		/// <param name="numSuccessiveFailures">The amount of previous successive failures to start the host process</param>
		/// <param name="hostProcessException">The exception caught and reported by the host process during startup - or null if the failure occured due to a timeout / connection problem</param>
		/// <param name="waitTime">The amount of time the <see cref="OutOfProcessSilo"/> shall wait before attempting to start the host process again</param>
		/// <returns></returns>
		Decision? OnStartFailure(
			int numSuccessiveFailures,
			Exception hostProcessException,
			out TimeSpan waitTime);

		/// <summary>
		/// This method is called when a failure in the host process or between the connection occured.
		/// </summary>
		/// <param name="failure">The type of failure that occurred</param>
		/// <returns>How the failure shall be resolved, or null if the <see cref="OutOfProcessSilo"/> shall decide</returns>
		Decision? OnFailure(Failure failure);

		/// <summary>
		/// This method is called when resolving the failure by restarting the host didn't work and <see cref="OutOfProcessSilo"/>
		/// had to give up.
		/// </summary>
		/// <remarks>
		/// This method is called before <see cref="OnResolutionFinished"/>.
		/// </remarks>
		/// <param name="failure"></param>
		/// <param name="decision"></param>
		/// <param name="exception"></param>
		void OnResolutionFailed(Failure failure, Decision decision, Exception exception);

		/// <summary>
		/// This method is called when the <see cref="OutOfProcessSilo"/> is finished with the failure resolution.
		/// This can indicate that the failure could be successfully resolved OR NOT, check the <paramref name="resolution"/>
		/// parameter to find out what happened.
		/// </summary>
		/// <param name="failure"></param>
		/// <param name="decision"></param>
		/// <param name="resolution"></param>
		void OnResolutionFinished(Failure failure, Decision decision, Resolution resolution);
	}
}