using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace SharpRemote.Tasks
{
	/// <summary>
	///     Executes scheduled delegates in a serial manner.
	/// </summary>
	/// <remarks>
	///     Only tasks with the proper access token are executed in a serial manner, all others are executed in parallel.
	/// </remarks>
	public sealed class SerialTaskScheduler
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly ManualResetEvent _disposeEvent;
		private readonly ConcurrentBag<Exception> _exceptions;
		private readonly SemaphoreSlim _pendingTaskCount;
		private readonly Queue<IPendingTask> _pendingTasks;
		private readonly object _syncRoot;
		private Task _executingTask;

		/// <summary>
		/// Initializes a new instance of this task scheduler.
		/// </summary>
		/// <param name="logExceptions"></param>
		public SerialTaskScheduler(bool logExceptions = false)
		{
			if (logExceptions)
				_exceptions = new ConcurrentBag<Exception>();
			_syncRoot = new object();
			_pendingTasks = new Queue<IPendingTask>();
			_disposeEvent = new ManualResetEvent(false);
			_pendingTaskCount = new SemaphoreSlim(0, int.MaxValue);
		}

		/// <summary>
		/// The list of all exceptions thrown during task execution.
		/// Is only captured when logExceptions was set to true upon construction.
		/// </summary>
		/// <remarks>
		/// Only used for testing.
		/// </remarks>
		public IEnumerable<Exception> Exceptions
		{
			get { return _exceptions ?? Enumerable.Empty<Exception>(); }
		}

		/// <summary>
		///     Tests if the task that executes all pending tasks is currently running or not.
		///     It will be stopped when no tasks have been queued for a certain time.
		/// </summary>
		internal bool IsExecutingTaskRunning
		{
			get { return _executingTask != null; }
		}

		public void Dispose()
		{
			_disposeEvent.Set();
		}

		private void ExecuteTasks()
		{
			while (true)
			{
				IPendingTask task = null;
				try
				{
					task = DequeueNextTask(TimeSpan.FromSeconds(10));
					if (task == null)
						break;

					task.Execute();
				}
				catch (Exception e)
				{
					if (_exceptions != null)
						_exceptions.Add(e);

					Log.ErrorFormat("Caught exception while executing task '{0}': {1}", task, e);
				}
			}
		}

		private IPendingTask DequeueNextTask(TimeSpan timeout)
		{
			var handles = new[]
				{
					_pendingTaskCount.AvailableWaitHandle,
					_disposeEvent
				};
			int idx = WaitHandle.WaitAny(handles, timeout);
			if (idx != 0)
			{
				lock (_syncRoot)
				{
					_executingTask = null;
				}

				return null;
			}

			lock (_syncRoot)
			{
				_pendingTaskCount.Wait();
				return _pendingTasks.Dequeue();
			}
		}

		public void QueueTask<T>(Func<T> fn, TaskCompletionSource<T> completionSource)
		{
			lock (_syncRoot)
			{
				var pendingTask = new PendingTask<T>(fn, completionSource);
				Task<T> task = pendingTask.Task;
				EnqueueTask(pendingTask, task);
				StarTaskIfNecessary();
			}
		}

		public Task<T> QueueTask<T>(Func<T> fn)
		{
			var completionSource = new TaskCompletionSource<T>();
			QueueTask(fn, completionSource);
			return completionSource.Task;
		}

		public void QueueTask(Action fn, TaskCompletionSource<int> completionSource)
		{
			lock (_syncRoot)
			{
				var pendingTask = new PendingTask(fn, completionSource);
				Task task = pendingTask.Task;
				EnqueueTask(pendingTask, task);
				StarTaskIfNecessary();
			}
		}

		public Task QueueTask(Action fn)
		{
			var completionSource = new TaskCompletionSource<int>();
			QueueTask(fn, completionSource);
			return completionSource.Task;
		}

		private void EnqueueTask(IPendingTask pendingTask, Task task)
		{
			_pendingTasks.Enqueue(pendingTask);
			_pendingTaskCount.Release(1);

			if (Log.IsDebugEnabled)
			{
				Log.DebugFormat("Queueing task '#{0}'", task.Id);
			}
		}

		private void StarTaskIfNecessary()
		{
			if (_executingTask == null)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Executing task is not (yet) running - starting it (again)");
				}

				_executingTask = new Task(ExecuteTasks);
				_executingTask.Start();
			}
		}

		private interface IPendingTask
		{
			void Execute();
		}

		private sealed class PendingTask
			: IPendingTask
		{
			private readonly TaskCompletionSource<int> _completionSource;
			private readonly Action _task;

			public PendingTask(Action task, TaskCompletionSource<int> completionSource)
			{
				if (task == null) throw new ArgumentNullException("task");
				if (completionSource == null) throw new ArgumentNullException("completionSource");

				_task = task;
				_completionSource = completionSource;
			}

			public Task Task
			{
				get { return _completionSource.Task; }
			}

			public void Execute()
			{
				try
				{
					_task();
					_completionSource.SetResult(0);
				}
				catch (Exception e)
				{
					_completionSource.SetException(e);
				}
			}
		}

		private sealed class PendingTask<T>
			: IPendingTask
		{
			private readonly TaskCompletionSource<T> _completionSource;
			private readonly Func<T> _task;

			public PendingTask(Func<T> task, TaskCompletionSource<T> completionSource)
			{
				if (task == null) throw new ArgumentNullException("task");
				if (completionSource == null) throw new ArgumentNullException("completionSource");

				_task = task;
				_completionSource = completionSource;
			}

			public Task<T> Task
			{
				get { return _completionSource.Task; }
			}

			public void Execute()
			{
				try
				{
					T result = _task();
					_completionSource.SetResult(result);
				}
				catch (Exception e)
				{
					_completionSource.SetException(e);
				}
			}
		}
	}
}