using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharpRemote.Hosting
{
	/// <summary>
	/// Responsible for invoking the heartbeat interface regularly.
	/// Notifies in case of skipped beats.
	/// </summary>
	internal sealed class HeartbeatMonitor
		: IDisposable
	{
		private readonly object _syncRoot;
		private readonly IHeartbeat _heartbeat;
		private readonly Task _task;
		private readonly TimeSpan _interval;
		private readonly bool _enabledWithAttachedDebugger;
		private readonly TimeSpan _failureInterval;
		private long _numHeartbeats;
		private DateTime? _lastHeartbeat;
		private volatile bool _isDisposed;
		private bool _failureDetected;

		public HeartbeatMonitor(IHeartbeat heartbeat,
		                        HeartbeatSettings settings)
			: this(heartbeat, settings.Interval, settings.SkippedHeartbeatThreshold, settings.ReportSkippedHeartbeatsAsFailureWithDebuggerAttached)
		{}

		public HeartbeatMonitor(IHeartbeat heartbeat, TimeSpan heartBeatInterval, int failureThreshold, bool enabledWithAttachedDebugger)
		{
			if (heartbeat == null) throw new ArgumentNullException("heartbeat");
			if (heartBeatInterval < TimeSpan.Zero) throw new ArgumentOutOfRangeException("heartBeatInterval");
			if (failureThreshold < 1) throw new ArgumentOutOfRangeException("failureThreshold");

			_syncRoot = new object();
			_heartbeat = heartbeat;
			_interval = heartBeatInterval;
			_enabledWithAttachedDebugger = enabledWithAttachedDebugger;
			_failureInterval = heartBeatInterval + TimeSpan.FromMilliseconds(failureThreshold*heartBeatInterval.TotalMilliseconds);
			_task = new Task(MeasureHeartbeats, TaskCreationOptions.LongRunning);
		}

		public TimeSpan Interval
		{
			get { return _interval; }
		}

		public TimeSpan FailureInterval
		{
			get { return _failureInterval; }
		}

		public void Start()
		{
			_task.Start();
		}

		private void MeasureHeartbeats()
		{
			while (!_isDisposed)
			{
				var started = DateTime.Now;
				var task = _heartbeat.Beat();
				if (!PerformHeartbeat(task))
				{
					ReportFailure();
					break;
				}

				_lastHeartbeat = DateTime.Now;

				lock (_syncRoot)
				{
					if (_isDisposed)
						break;

					++_numHeartbeats;
				}

				var elapsed = DateTime.Now - started;
				var remainingSleep = _interval - elapsed;
				if (remainingSleep > TimeSpan.Zero)
					Thread.Sleep(remainingSleep);
			}
		}

		/// <summary>
		/// Performs a single heartbeat.
		/// </summary>
		/// <param name="task"></param>
		/// <returns>True when the heartbeat succeeded, false otherwise</returns>
		private bool PerformHeartbeat(Task task)
		{
			if (task == null)
			{
				return false;
			}

			try
			{
				if (!task.Wait(_failureInterval) && _enabledWithAttachedDebugger)
				{
					return false;
				}
			}
			catch (AggregateException)
			{
				return false;
			}

			if (task.IsFaulted)
			{
				return false;
			}
			return true;
		}

		public long NumHeartbeats
		{
			get
			{
				lock (_syncRoot)
				{
					return _numHeartbeats;
				}
			}
		}

		public DateTime? LastHeartbeat
		{
			get { return _lastHeartbeat; }
		}

		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		public bool FailureDetected
		{
			get { return _failureDetected; }
		}

		private void ReportFailure()
		{
			_failureDetected = true;
			var fn = OnFailure;
			if (fn != null)
				fn();
		}

		/// <summary>
		/// This event is fired when and if this monitor detects a failure of the heartbeat
		/// interface because too many heartbeats passed
		/// </summary>
		public event Action OnFailure;

		public void Dispose()
		{
			lock (_syncRoot)
			{
				_isDisposed = false;
			}
		}
	}
}