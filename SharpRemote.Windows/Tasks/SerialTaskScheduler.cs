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
	/// Executes all scheduled tasks in a serial manner.
	/// </summary>
	/// <remarks>
	/// Only tasks with the proper access token are executed in a serial manner, all others are executed in parallel.
	/// </remarks>
	public sealed class SerialTaskScheduler
		: TaskScheduler
		, IDisposable
	{
		/// <summary>
		/// Is used to permit tasks being scheduled with this scheduler.
		/// Exists because if a task already being scheduled by this scheduler spawns a second task,
		/// then that will use the CURRENT scheduler for execution which causes a deadlock.
		/// </summary>
		internal static readonly object AccessToken;

		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Queue<Task> _pendingTasks;
		private readonly SemaphoreSlim _pendingTaskCount;
		private readonly ManualResetEvent _disposeEvent;
		private readonly object _syncRoot;
		private Task _executingTask;
		private readonly ConcurrentBag<Exception> _exceptions;

		static SerialTaskScheduler()
		{
			AccessToken = new object();
		}

		public IEnumerable<Exception> Exceptions
		{
			get { return _exceptions ?? Enumerable.Empty<Exception>(); }
		}

		public SerialTaskScheduler(bool logExceptions = false)
		{
			if (logExceptions)
				_exceptions = new ConcurrentBag<Exception>();
			_syncRoot = new object();
			_pendingTasks = new Queue<Task>();
			_disposeEvent = new ManualResetEvent(false);
			_pendingTaskCount = new SemaphoreSlim(0, int.MaxValue);
		}

		/// <summary>
		/// Tests if the task that executes all pending tasks is currently running or not.
		/// It will be stopped when no tasks have been queued for a certain time.
		/// </summary>
		internal bool IsExecutingTaskRunning
		{
			get { return _executingTask != null; }
		}

		public override int MaximumConcurrencyLevel
		{
			get { return 1; }
		}

		private void ExecuteTasks()
		{
			while (true)
			{
				Task task = null;
				try
				{
					task = DequeueNextTask(TimeSpan.FromSeconds(10));
					if (task == null)
						break;

					ExecuteTask(task);
				}
				catch (Exception e)
				{
					if (_exceptions != null)
						_exceptions.Add(e);

					Log.ErrorFormat("Caught exception while executing task '{0}': {1}", task, e);
				}
			}
		}

		private Task DequeueNextTask(TimeSpan timeout)
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

		protected override void QueueTask(Task task)
		{
			if (task.AsyncState != AccessToken)
			{
				var t = new Thread(ExecuteNonSerialTask);
				t.Start(task);
			}
			else
			{
				lock (_syncRoot)
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("Queueing task '#{0}'", task.Id);
					}

					_pendingTasks.Enqueue(task);
					_pendingTaskCount.Release(1);

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
			}
		}

		private void ExecuteNonSerialTask(object state)
		{
			var task = (Task) state;
			ExecuteTask(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		private void ExecuteTask(Task task)
		{
			if (!TryExecuteTask(task))
			{
				// Programming error
				throw new NotImplementedException();
			}
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			lock (_syncRoot)
			{
				return _pendingTasks.ToList();
			}
		}

		public void Dispose()
		{
			_disposeEvent.Set();
		}
	}
}