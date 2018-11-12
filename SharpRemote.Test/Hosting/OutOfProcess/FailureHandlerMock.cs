using System;
using System.Collections.Generic;
using System.Linq;
using SharpRemote.Hosting.OutOfProcess;

namespace SharpRemote.Test.Hosting.OutOfProcess
{
	public sealed class FailureHandlerMock
		: IFailureHandler
	{
		private readonly object _syncRoot;
		private readonly List<Failure> _failures;

		public FailureHandlerMock()
		{
			_syncRoot = new object();
			_failures = new List<Failure>();
		}

		public void Clear()
		{
			lock (_syncRoot)
			{
				NumStartFailure = 0;
				NumResolutionFailed = 0;
				NumResolutionFinished = 0;
				_failures.Clear();
			}
		}

		public int NumStartFailure { get; private set; }

		public int NumFailure
		{
			get
			{
				lock (_syncRoot)
				{
					return _failures.Count;
				}
			}
		}

		public int NumResolutionFailed { get; private set; }
		public int NumResolutionFinished { get; private set; }

		public IReadOnlyList<Failure> Failures
		{
			get
			{
				lock (_syncRoot)
				{
					return _failures.ToList();
				}
			}
		}

		#region Implementation of IFailureHandler

		public Decision? OnStartFailure(int numSuccessiveFailures, Exception hostProcessException, out TimeSpan waitTime)
		{
			++NumStartFailure;

			waitTime = TimeSpan.Zero;
			return null;
		}

		public Decision? OnFailure(Failure failure)
		{
			lock (_syncRoot)
			{
				_failures.Add(failure);
			}

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