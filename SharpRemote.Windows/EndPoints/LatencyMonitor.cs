using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This class is responsible for measuring the average latency of a <see cref="ILatency.Roundtrip()"/>
	/// invocation. It can be used by installing a <see cref="ILatency"/> proxy on the side that wants to
	/// measure the latency and a <see cref="Latency"/> servant on the other side.
	/// </summary>
	public sealed class LatencyMonitor
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly TimeSpan _interval;
		private readonly ILatency _latencyGrain;
		private readonly RingBuffer<TimeSpan> _measurements;
		private readonly object _syncRoot;
		private readonly Task _task;

		private volatile bool _isDisposed;
		private TimeSpan _roundTripTime;

		/// <summary>
		/// Initializes this latency monitor with the given interval and number of samples over which
		/// the average latency is determined.
		/// </summary>
		/// <param name="latencyGrain"></param>
		/// <param name="interval"></param>
		/// <param name="numSamples"></param>
		public LatencyMonitor(
			ILatency latencyGrain,
			TimeSpan interval,
			int numSamples
			)
		{
			if (latencyGrain == null) throw new ArgumentNullException("latencyGrain");
			if (interval < TimeSpan.Zero) throw new ArgumentOutOfRangeException("interval", "A positive interval must be given");
			if (numSamples < 1) throw new ArgumentOutOfRangeException("numSamples", "1 or more samples must be specified");

			_syncRoot = new object();
			_interval = interval;
			_latencyGrain = latencyGrain;
			_measurements = new RingBuffer<TimeSpan>(numSamples);
			_task = new Task(MeasureLatencyLoop, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Initializes this latency monitor with the given interval and number of samples over which
		/// the average latency is determined.
		/// </summary>
		/// <param name="latencyGrain"></param>
		/// <param name="settings"></param>
		public LatencyMonitor(ILatency latencyGrain, LatencySettings settings)
			: this(latencyGrain,
			       settings.Interval,
			       settings.NumSamples)
		{
		}

		/// <summary>
		/// The average roundtrip time of a <see cref="ILatency.Roundtrip()"/> call.
		/// Can be used to determine the base overhead of the remoting system.
		/// </summary>
		public TimeSpan RoundTripTime
		{
			get
			{
				lock (_syncRoot)
				{
					return _roundTripTime;
				}
			}
		}

		public void Dispose()
		{
			_isDisposed = true;
		}

		/// <summary>
		/// Starts this latency monitor, e.g. begins measuring the latency.
		/// </summary>
		public void Start()
		{
			_task.Start();
		}

		private void MeasureLatencyLoop()
		{
			var sw = new Stopwatch();
			while (!_isDisposed)
			{
				TimeSpan toSleep;
				if (!MeasureLatency(sw, out toSleep))
					break;

				if (toSleep > TimeSpan.Zero)
				{
					Thread.Sleep(toSleep);
				}
			}
		}

		/// <summary>
		///     Measures and stores the current latencyGrain and returns the amount of time
		///     the calling thread should sleep in order to repeat measurements at
		///     <see cref="_interval" />.
		/// </summary>
		/// <param name="sw"></param>
		/// <param name="toSleep"></param>
		private bool MeasureLatency(Stopwatch sw, out TimeSpan toSleep)
		{
			try
			{
				sw.Restart();
				_latencyGrain.Roundtrip();
				sw.Stop();
				TimeSpan rtt = sw.Elapsed;

				_measurements.Enqueue(rtt);
				TimeSpan averageRtt = TimeSpan.FromTicks((long) (((double)_measurements.Sum(x => x.Ticks))/_measurements.Length));

				lock (_syncRoot)
				{
					_roundTripTime = averageRtt;
				}

				toSleep = _interval - rtt;
				return true;
			}
			catch (NotConnectedException)
			{
				toSleep = TimeSpan.Zero;
				return false;
			}
			catch (ConnectionLostException)
			{
				toSleep = TimeSpan.Zero;
				return false;
			}
			catch (Exception e)
			{
				Log.ErrorFormat("Caught unexpected exception while measureing latency: {0}", e);
				toSleep = _interval;
				return true;
			}
		}
	}
}