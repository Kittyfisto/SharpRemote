using System;
using SharpRemote.Hosting.OutOfProcess;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	public sealed class FailureHandlerMock
		: IFailureHandler
	{
		public int NumStartFailure { get; private set; }
		public int NumFailure { get; private set; }
		public int NumResolutionFailed { get; private set; }
		public int NumResolutionFinished { get; private set; }

		#region Implementation of IFailureHandler

		public Decision? OnStartFailure(int numSuccessiveFailures, Exception hostProcessException, out TimeSpan waitTime)
		{
			++NumStartFailure;

			waitTime = TimeSpan.Zero;
			return null;
		}

		public Decision? OnFailure(Failure failure)
		{
			++NumFailure;

			return null;
		}

		public void OnResolutionFailed(Failure failure, Decision decision, Exception exception)
		{
			++NumResolutionFailed;
		}

		public void OnResolutionFinished(Failure failure, Decision decision, Resolution resolution)
		{
			++NumResolutionFinished;
		}

		#endregion
	}
}