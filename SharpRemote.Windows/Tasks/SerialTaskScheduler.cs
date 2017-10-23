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

		/// <summary>
		///     The amount of time that must pass without having done any work before the thread is shut-down.
		/// </summary>
		private static readonly TimeSpan DeactivationThreshold = TimeSpan.FromSeconds(10);

		private readonly ManualResetEvent _disposeEvent;
		private readonly SemaphoreSlim _pendingTaskCount;
		private readonly Queue<IPendingTask> _pendingTasks;
		private readonly object _syncRoot;
		private readonly TimeSpan _deactivationThreshold;
		private Thread _executingThread;

		#region Debugging

		private readonly ConcurrentBag<Exception> _exceptions;
		private readonly string _methodName;
		private readonly string _name;
		private readonly long? _objectId;
		private readonly string _typeName;

		#endregion

		private SerialTaskScheduler(bool logExceptions)
		{
			if (logExceptions)
				_exceptions = new ConcurrentBag<Exception>();

			_syncRoot = new object();
			_pendingTasks = new Queue<IPendingTask>();
			_disposeEvent = new ManualResetEvent(false);
			_pendingTaskCount = new SemaphoreSlim(0, int.MaxValue);
		}

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="deactivationThreshold"></param>
		/// <param name="logExceptions"></param>
		public SerialTaskScheduler(TimeSpan deactivationThreshold, bool logExceptions)
			: this(logExceptions)
		{
			if (deactivationThreshold <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(deactivationThreshold));

			_deactivationThreshold = deactivationThreshold;
		}

		/// <summary>
		///     Initializes a new instance of this task scheduler.
		/// </summary>
		/// <param name="methodName"></param>
		/// <param name="objectId"></param>
		/// <param name="logExceptions"></param>
		/// <param name="typeName"></param>
		public SerialTaskScheduler(string typeName = null,
		                           string methodName = null,
		                           long? objectId = null,
		                           bool logExceptions = false)
			: this(logExceptions)
		{
			_deactivationThreshold = DeactivationThreshold;
			_typeName = typeName;
			_methodName = methodName;
			_objectId = objectId;

			if (_typeName != null)
			{
				if (_methodName != null)
				{
					_name = string.Format("{0}.{1}() (#{2})", _typeName, _methodName, _objectId);
				}
				else
				{
					_name = _objectId != null
						        ? string.Format("{0} (#{1})", _typeName, _objectId)
						        : string.Format("{0}", _typeName);
				}
			}
		}

		/// <summary>
		///     The list of all exceptions thrown during task execution.
		///     Is only captured when logExceptions was set to true upon construction.
		/// </summary>
		/// <remarks>
		///     Only used for testing.
		/// </remarks>
		public IEnumerable<Exception> Exceptions => _exceptions ?? Enumerable.Empty<Exception>();

		/// <summary>
		///     Tests if the task that executes all pending tasks is currently running or not.
		///     It will be stopped when no tasks have been queued for a certain time.
		/// </summary>
		internal bool IsExecutingThreadRunning => _executingThread != null;

		/// <inheritdoc />
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
					task = DequeueNextTask(_deactivationThreshold);
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

			switch (idx)
			{
				case WaitHandle.WaitTimeout:
					lock (_syncRoot)
					{
						Log.DebugFormat("No tasks were scheduled in the last {0}s, shutting thread down...", timeout.TotalSeconds);
						_executingThread = null;
					}

					return null;

				case 0:
					lock (_syncRoot)
					{
						_pendingTaskCount.Wait();
						return _pendingTasks.Dequeue();
					}

				case 1:
					lock (_syncRoot)
					{
						Log.DebugFormat("Scheduler was disposed of, shutting thread down...");
						_executingThread = null;
					}
					return null;

				default:
					throw new NotImplementedException(string.Format("Unexpected index returned from WaitHandle.WaitAny(): {0}", idx));
			}
		}

		/// <summary>
		///     Enqueues the given task to this scheduler: It will be executed
		///     once all previous tasks have finished or failed.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fn"></param>
		/// <param name="completionSource"></param>
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

		/// <summary>
		///     Enqueues the given task to this scheduler: It will be executed
		///     once all previous tasks have finished or failed.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="fn"></param>
		public Task<T> QueueTask<T>(Func<T> fn)
		{
			var completionSource = new TaskCompletionSource<T>();
			QueueTask(fn, completionSource);
			return completionSource.Task;
		}

		/// <summary>
		///     Enqueues the given task to this scheduler: It will be executed
		///     once all previous tasks have finished or failed.
		/// </summary>
		/// <param name="fn"></param>
		/// <param name="completionSource"></param>
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

		/// <summary>
		///     Enqueues the given task to this scheduler: It will be executed
		///     once all previous tasks have finished or failed.
		/// </summary>
		/// <param name="fn"></param>
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
			if (_executingThread == null)
			{
				if (Log.IsDebugEnabled)
				{
					Log.Debug("Executing thread is not (yet) running - starting it (again)");
				}

				_executingThread = new Thread(ExecuteTasks) {Name = _name};
				_executingThread.Start();
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
				if (task == null) throw new ArgumentNullException(nameof(task));
				if (completionSource == null) throw new ArgumentNullException(nameof(completionSource));

				_task = task;
				_completionSource = completionSource;
			}

			public Task Task => _completionSource.Task;

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
				if (task == null) throw new ArgumentNullException(nameof(task));
				if (completionSource == null) throw new ArgumentNullException(nameof(completionSource));

				_task = task;
				_completionSource = completionSource;
			}

			public Task<T> Task => _completionSource.Task;

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