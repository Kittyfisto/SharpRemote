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
		/// <summary>
		/// This event is fired when <see cref="OnResolutionFailed"/> is called.
		/// </summary>
		public event Action OnResolutionFailedEvent;

		/// <inheritdoc />
		public Decision? OnStartFailure(int numSuccessiveFailures, Exception hostProcessException, out TimeSpan waitTime)
		{
			waitTime = TimeSpan.Zero;
			return Decision.Stop;
		}

		/// <inheritdoc />
		public Decision? OnFailure(Failure failure)
		{
			return Decision.Stop;
		}

		/// <inheritdoc />
		public void OnResolutionFailed(Failure failure, Decision decision, Exception exception)
		{
			var fn = OnResolutionFailedEvent;
			fn?.Invoke();
		}

		/// <inheritdoc />
		public void OnResolutionFinished(Failure failure, Decision decision, Resolution resolution)
		{
			
		}
	}
}