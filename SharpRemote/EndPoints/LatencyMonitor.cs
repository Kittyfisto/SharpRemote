using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	///     This class is responsible for measuring the average latency of a <see cref="ILatency.Roundtrip()" />
	///     invocation. It can be used by installing a <see cref="ILatency" /> proxy on the side that wants to
	///     measure the latency and a <see cref="Latency" /> servant on the other side.
	/// </summary>
	internal sealed class LatencyMonitor
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private readonly string _endPointName;

		private readonly TimeSpan _interval;
		private readonly ILatency _latencyGrain;
		private readonly TimeSpanStatisticsContainer _measurements;
		private readonly bool _performLatencyMeasurements;
		private readonly object _syncRoot;
		private readonly Stopwatch _stopwatch;

		private volatile bool _isDisposed;

		private Timer _timer;
		private bool _isStarted;

		/// <summary>
		///     Initializes this latency monitor with the given interval and number of samples over which
		///     the average latency is determined.
		/// </summary>
		/// <param name="latencyGrain"></param>
		/// <param name="interval"></param>
		/// <param name="numSamples"></param>
		/// <param name="performLatencyMeasurements"></param>
		/// <param name="endPointName"></param>
		public LatencyMonitor(
			ILatency latencyGrain,
			TimeSpan interval,
			int numSamples,
			bool performLatencyMeasurements,
			string endPointName = null
		)
		{
			if (latencyGrain == null) throw new ArgumentNullException(nameof(latencyGrain));
			if (interval < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(interval), "A positive interval must be given");
			if (numSamples < 1) throw new ArgumentOutOfRangeException(nameof(numSamples), "1 or more samples must be specified");

			_syncRoot = new object();
			_interval = interval;
			_performLatencyMeasurements = performLatencyMeasurements;
			_latencyGrain = latencyGrain;
			_measurements = new TimeSpanStatisticsContainer(numSamples);
			_endPointName = endPointName;
			_stopwatch = new Stopwatch();
		}

		/// <summary>
		///     Initializes this latency monitor with the given interval and number of samples over which
		///     the average latency is determined.
		/// </summary>
		/// <param name="latencyGrain"></param>
		/// <param name="settings"></param>
		/// <param name="endPointName"></param>
		public LatencyMonitor(ILatency latencyGrain,
		                      LatencySettings settings,
		                      string endPointName = null)
			: this(latencyGrain,
			       settings.Interval,
			       settings.NumSamples,
			       settings.PerformLatencyMeasurements,
			       endPointName)
		{
		}

		/// <summary>
		///     The average roundtrip time of a <see cref="ILatency.Roundtrip()" /> call.
		///     Can be used to determine the base overhead of the remoting system.
		/// </summary>
		public TimeSpan RoundtripTime
		{
			get
			{
				lock (_syncRoot)
				{
					return _measurements.Average;
				}
			}
		}

		/// <summary>
		///     Whether or not this latency monitor has been disposed of.
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		///     Whether or not <see cref="Start()" /> has been called (and <see cref="Stop()" /> has not since then).
		/// </summary>
		public bool IsStarted => _isStarted;

		public void Dispose()
		{
			Stop();
			_isDisposed = true;
		}

		/// <summary>
		///     Starts this latency monitor, e.g. begins measuring the latency.
		/// </summary>
		public void Start()
		{
			_isStarted = true;
			if (_performLatencyMeasurements)
			{
				_timer = new Timer(OnUpdate, null, _interval, _interval);
			}
		}

		/// <summary>
		///     Stops the latency monitor from perform any further measurements.
		/// </summary>
		public void Stop()
		{
			_isStarted = false;
			_timer?.Dispose();
		}
		
		private void OnUpdate(object state)
		{
			MeasureLatency();
		}

		/// <summary>
		///     Measures the current latency and calculates the average latency.
		/// </summary>
		internal bool MeasureLatency()
		{
			try
			{
				_stopwatch.Restart();
				_latencyGrain.Roundtrip();
				_stopwatch.Stop();
				var rtt = _stopwatch.Elapsed;

				lock (_syncRoot)
				{
					_measurements.Enqueue(rtt);
				}

				return true;
			}
			catch (NotConnectedException e)
			{
				Log.DebugFormat("{0}: Caught exception while measuring latency: {1}", _endPointName, e);
				return false;
			}
			catch (ConnectionLostException e)
			{
				Log.DebugFormat("{0}: Caught exception while measuring latency: {1}", _endPointName, e);
				return false;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("{0}: Caught unexpected exception while measuring latency: {1}", _endPointName, e);
				return true;
			}
		}
	}
}