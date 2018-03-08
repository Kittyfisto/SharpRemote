using System;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharpRemote.Diagnostics;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     Responsible for invoking the heartbeat interface regularly.
	///     Notifies in case of skipped beats.
	/// </summary>
	internal sealed class HeartbeatMonitor
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly ConnectionId _connectionId;

		private readonly IDebugger _debugger;
		private readonly bool _enabledWithAttachedDebugger;
		private readonly TimeSpan _failureInterval;
		private readonly IHeartbeat _heartbeat;
		private readonly TimeSpan _interval;
		private readonly string _endPointName;
		private readonly EndPoint _localEndPoint;
		private readonly EndPoint _remoteEndPoint;
		private readonly object _syncRoot;
		private readonly Task _task;
		private readonly bool _useHeartbeatFailureDetection;
		private readonly bool _allowRemoteHeartbeatDisable;

		private bool _failureDetected;
		private volatile bool _isDisposed;
		private bool _isStarted;
		private DateTime? _lastHeartbeat;
		private long _numHeartbeats;
		private volatile bool _remoteIsDebuggerAttached;

		/// <summary>
		///     Initializes this heartbeat monitor with the given heartbeat interface and
		///     settings that define how often a heartbeat measurement is performed.
		/// </summary>
		/// <param name="heartbeat"></param>
		/// <param name="debugger"></param>
		/// <param name="settings"></param>
		/// <param name="connectionId"></param>
		/// <param name="endPointName"></param>
		/// <param name="localEndPoint"></param>
		/// <param name="remoteEndPoint"></param>
		public HeartbeatMonitor(IHeartbeat heartbeat,
		                        IDebugger debugger,
		                        HeartbeatSettings settings,
		                        ConnectionId connectionId,
		                        string endPointName,
		                        EndPoint localEndPoint,
		                        EndPoint remoteEndPoint)
			: this(
				heartbeat,
				debugger,
				settings.Interval,
				settings.SkippedHeartbeatThreshold,
				settings.ReportSkippedHeartbeatsAsFailureWithDebuggerAttached,
				settings.UseHeartbeatFailureDetection,
				settings.AllowRemoteHeartbeatDisable,
				connectionId,
				endPointName,
				localEndPoint,
				remoteEndPoint)
		{
		}

		/// <summary>
		///     Initializes this heartbeat monitor with the given heartbeat interface and
		///     settings that define how often a heartbeat measurement is performed.
		/// </summary>
		/// <param name="heartbeat"></param>
		/// <param name="debugger"></param>
		/// <param name="heartBeatInterval"></param>
		/// <param name="failureThreshold"></param>
		/// <param name="enabledWithAttachedDebugger"></param>
		/// <param name="useHeartbeatFailureDetection"></param>
		/// <param name="allowRemoteHeartbeatDisable"></param>
		/// <param name="connectionId"></param>
		/// <param name="endPointName"></param>
		/// <param name="locEndPoint"></param>
		/// <param name="remoteEndPoint"></param>
		public HeartbeatMonitor(IHeartbeat heartbeat,
		                        IDebugger debugger,
		                        TimeSpan heartBeatInterval,
		                        int failureThreshold,
		                        bool enabledWithAttachedDebugger,
		                        bool useHeartbeatFailureDetection,
		                        bool allowRemoteHeartbeatDisable,
		                        ConnectionId connectionId,
		                        string endPointName,
		                        EndPoint locEndPoint,
		                        EndPoint remoteEndPoint)
		{
			if (heartbeat == null) throw new ArgumentNullException(nameof(heartbeat));
			if (debugger == null) throw new ArgumentNullException(nameof(debugger));
			if (heartBeatInterval < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(heartBeatInterval));
			if (failureThreshold < 1) throw new ArgumentOutOfRangeException(nameof(failureThreshold));
			if (connectionId == ConnectionId.None) throw new ArgumentException("connectionId");
			if (remoteEndPoint == null) throw new ArgumentNullException(nameof(remoteEndPoint));

			_syncRoot = new object();
			_heartbeat = heartbeat;
			_heartbeat.RemoteDebuggerAttached += OnRemoteDebuggerAttached;
			_heartbeat.RemoteDebuggerDetached += OnRemoteDebuggerDetached;

			_debugger = debugger;
			_interval = heartBeatInterval;
			_enabledWithAttachedDebugger = enabledWithAttachedDebugger;
			_useHeartbeatFailureDetection = useHeartbeatFailureDetection;
			_allowRemoteHeartbeatDisable = allowRemoteHeartbeatDisable;
			_connectionId = connectionId;
			_endPointName = endPointName;
			_localEndPoint = locEndPoint;
			_remoteEndPoint = remoteEndPoint;
			_failureInterval = heartBeatInterval +
			                   TimeSpan.FromMilliseconds(failureThreshold*heartBeatInterval.TotalMilliseconds);
			_task = new Task(MeasureHeartbeats, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		///     The configured heartbeat interval, e.g. the amount of time that shall pass before
		///     a new heartbeat is started.
		/// </summary>
		public TimeSpan Interval => _interval;

		/// <summary>
		///     The amount of time for which a heartbeat may not return (e.g. fail) before the connection is assumed
		///     to be dead.
		/// </summary>
		public TimeSpan FailureInterval => _failureInterval;

		/// <summary>
		///     The total number of heartbeats performed since <see cref="Start" />.
		/// </summary>
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

		/// <summary>
		///     The point in time where the last heartbeat was performed.
		/// </summary>
		public DateTime? LastHeartbeat => _lastHeartbeat;

		/// <summary>
		///     Whether or not this heartbeat is disposed of.
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		///     Whether or not <see cref="Start()" /> has been called (and <see cref="Stop()" /> has not since then).
		/// </summary>
		public bool IsStarted => _isStarted;

		/// <summary>
		///     Whether or not a failure has been detected.
		/// </summary>
		public bool FailureDetected => _failureDetected;

		/// <summary>
		///     Whether or not a failure shall currently be reported - or ignored
		/// </summary>
		/// <returns>True when a failure shall be reported, false when it shall be ignored</returns>
		public bool ReportFailures
		{
			get
			{
				if (!_useHeartbeatFailureDetection)
					return false;

				if (!_enabledWithAttachedDebugger)
				{
					//< Failures shall be ignored when a debugger is attached
					if (_debugger.IsDebuggerAttached)
						return false; //< A debugger is attached to THIS process

					if (_remoteIsDebuggerAttached && _allowRemoteHeartbeatDisable)
						return false; //< A debugger is attached to the process of the remote endpoint
				}

				//< Failures shall always be reported, even when the debugger is attached
				return true;
			}
		}

		public void Dispose()
		{
			lock (_syncRoot)
			{
				_isStarted = false;
				_isDisposed = true;
			}
		}

		private void OnRemoteDebuggerAttached()
		{
			var before = ReportFailures;
			_remoteIsDebuggerAttached = true;

			var message = new StringBuilder();
			message.AppendFormat("{0}: A debugger has been attached to the remote endpoint's process: {1}",
			                     _endPointName,
			                     _remoteEndPoint);
			message.Append(", heartbeat failures will ");
			if (!ReportFailures)
			{
				message.Append(before != ReportFailures ? "now be ignored" : "still be ignored");
			}
			else
			{
				message.Append(before != ReportFailures ? "be reportd again" : "still be reported");
			}

			Log.Info(message);
		}

		private void OnRemoteDebuggerDetached()
		{
			var before = ReportFailures;
			_remoteIsDebuggerAttached = false;

			var message = new StringBuilder();
			message.AppendFormat("{0}: A debugger has been detached from the remote endpoint's process: {1}",
			                     _endPointName,
			                     _remoteEndPoint);
			message.Append(", heartbeat failures will ");
			if (!ReportFailures)
			{
				message.Append(before != ReportFailures ? "now be ignored" : "still be ignored");
			}
			else
			{
				message.Append(before != ReportFailures ? "be reported again" : "still be reported");
			}

			Log.Info(message);
		}

		/// <summary>
		///     Starts this heartbeat monitor.
		/// </summary>
		/// <remarks>
		///     Resets the <see cref="FailureDetected" /> property to false.
		/// </remarks>
		public void Start()
		{
			lock (_syncRoot)
			{
				_failureDetected = false;
				_isStarted = true;
			}

			if (_useHeartbeatFailureDetection)
			{
				_task.Start();
			}
		}

		/// <summary>
		///     Stops the heartbeat monitor, failures will no longer be reported, nor
		///     will the proxy be accessed in any way.
		/// </summary>
		public void Stop()
		{
			lock (_syncRoot)
			{
				_isStarted = false;
			}
		}

		private void MeasureHeartbeats()
		{
			while (_isStarted)
			{
				try
				{
					DateTime started = DateTime.Now;
					if (!PerformHeartbeat())
						break;

					_lastHeartbeat = DateTime.Now;

					lock (_syncRoot)
					{
						if (_isDisposed)
							break;

						++_numHeartbeats;
					}

					TimeSpan elapsed = DateTime.Now - started;
					TimeSpan remainingSleep = _interval - elapsed;
					if (remainingSleep > TimeSpan.Zero)
						Thread.Sleep(remainingSleep);
				}
				catch (Exception e)
				{
					Log.ErrorFormat("{0}: {1} to {2}, caught unexpected exception: {3}",
					                _endPointName,
					                _localEndPoint,
					                _remoteEndPoint,
					                e);
				}
			}
		}

		private bool PerformHeartbeat()
		{
			Task task;
			try
			{
				Log.DebugFormat("{0}: {1} to {2}, sending heartbeat...",
				                _endPointName,
				                _localEndPoint,
				                _remoteEndPoint);

				task = _heartbeat.Beat();
				task.ContinueWith(ObserverException, TaskContinuationOptions.OnlyOnFaulted);
			}
			catch (NotConnectedException)
			{
				return false;
			}
			catch (ConnectionLostException)
			{
				return false;
			}

			if (!WaitForHeartbeat(task))
			{
				ReportFailure();
				return false;
			}

			return true;
		}

		private void ObserverException(Task task, object unused)
		{
			AggregateException exception = task.Exception;
			if (Log.IsDebugEnabled && exception != null)
			{
				Log.DebugFormat("{0}: {1} to {2}, Task (finally) threw exception - ignoring it: {3}",
				                _endPointName,
				                _localEndPoint,
				                _remoteEndPoint,
				                exception);
			}
		}

		/// <summary>
		///     Performs a single heartbeat.
		/// </summary>
		/// <param name="task"></param>
		/// <returns>True when the heartbeat succeeded, false otherwise</returns>
		private bool WaitForHeartbeat(Task task)
		{
			if (task == null)
			{
				return false;
			}

			try
			{
				if (!task.Wait(_failureInterval))
				{
					if (ReportFailures)
					{
						Log.DebugFormat("{0}: {1} to {2}, heartbeat failed",
						                _endPointName,
						                _localEndPoint,
						                _remoteEndPoint);
						return false;
					}

					Log.InfoFormat("{0}: {1} to {2}, ignoring heartbeat failure",
					               _endPointName,
					               _localEndPoint,
					               _remoteEndPoint);
				}
				else
				{
					if (Log.IsDebugEnabled)
					{
						Log.DebugFormat("{0}: {1} to {2}, heartbeat succeeded",
						                _endPointName,
						                _localEndPoint,
						                _remoteEndPoint);
					}
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

		private void ReportFailure()
		{
			lock (_syncRoot)
			{
				if (_isDisposed)
					return;

				if (!_isStarted)
					return;
			}

			_failureDetected = true;
			OnFailure?.Invoke(_connectionId);
		}

		/// <summary>
		///     This event is fired when and if this monitor detects a failure of the heartbeat
		///     interface because too many heartbeats passed
		/// </summary>
		public event Action<ConnectionId> OnFailure;
	}
}