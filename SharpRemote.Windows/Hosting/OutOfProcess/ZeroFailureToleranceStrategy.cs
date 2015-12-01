using System;

namespace SharpRemote.Hosting.OutOfProcess
{
	/// <summary>
	/// A <see cref="IFailureHandler"/> implementation that doesn't tolerate any kind of failure and
	/// immediately stops the <see cref="OutOfProcessSilo"/>.
	/// </summary>
	public sealed class ZeroFailureToleranceStrategy
		: IFailureHandler
	{
		public event Action OnResolutionFailedEvent;

		public Decision? OnStartFailure(int numSuccessiveFailures, Exception hostProcessException, out TimeSpan waitTime)
		{
			waitTime = TimeSpan.Zero;
			return Decision.Stop;
		}

		public Decision? OnFailure(Failure failure)
		{
			return Decision.Stop;
		}

		public void OnResolutionFailed(Failure failure, Decision decision, Exception exception)
		{
			var fn = OnResolutionFailedEvent;
			if (fn != null)
				fn();
		}

		public void OnResolutionFinished(Failure failure, Decision decision, Resolution resolution)
		{
			
		}
	}
}