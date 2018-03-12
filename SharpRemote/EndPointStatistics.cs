using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;

namespace SharpRemote
{
	/// <summary>
	///     Responsible for retrieving statistics about a <see cref="IRemotingEndPoint" /> and printing
	///     them to a <see cref="ILog" />.
	/// </summary>
	internal sealed class EndPointStatistics
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly StatisticsContainer _bytesReceived;
		private readonly StatisticsContainer _bytesSent;

		private readonly IRemotingEndPoint _endPoint;
		private readonly TimeSpanStatisticsContainer _gcTime;
		private readonly StatisticsContainer _messagesReceived;
		private readonly StatisticsContainer _messagesSent;
		private readonly StatisticsContainer _proxiesCollected;
		private readonly StatisticsContainer _servantsCollected;

		private Timer _timer;

		private TimeSpan _lastGcTime;
		private long _lastNumBytesReceived;

		private long _lastNumBytesSent;
		private long _lastNumMessagesReceived;
		private long _lastNumMessagesSent;
		private long _lastNumProxiesCollected;

		private long _lastNumServantsCollected;

		public EndPointStatistics(IRemotingEndPoint endPoint)
		{
			if (endPoint == null)
				throw new ArgumentNullException(nameof(endPoint));

			_endPoint = endPoint;

			const int numSamples = 60;
			_bytesSent = new StatisticsContainer(numSamples);
			_bytesReceived = new StatisticsContainer(numSamples);
			_messagesSent = new StatisticsContainer(numSamples);
			_messagesReceived = new StatisticsContainer(numSamples);
			_servantsCollected = new StatisticsContainer(numSamples);
			_proxiesCollected = new StatisticsContainer(numSamples);
			_gcTime = new TimeSpanStatisticsContainer(numSamples);
		}

		public void Start()
		{
			var tick = TimeSpan.FromSeconds(value: 1);
			_timer = new Timer(OnUpdate, state: null, dueTime: tick, period: tick);
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		private void OnUpdate(object state)
		{
			Update();
		}

		internal void Update()
		{
			AppendDelta(_endPoint.NumBytesSent, ref _lastNumBytesSent, _bytesSent);
			AppendDelta(_endPoint.NumBytesReceived, ref _lastNumBytesReceived, _bytesReceived);
			AppendDelta(_endPoint.NumMessagesSent, ref _lastNumMessagesSent, _messagesSent);
			AppendDelta(_endPoint.NumMessagesReceived, ref _lastNumMessagesReceived, _messagesReceived);
			AppendDelta(_endPoint.NumServantsCollected, ref _lastNumServantsCollected, _servantsCollected);
			AppendDelta(_endPoint.NumProxiesCollected, ref _lastNumProxiesCollected, _proxiesCollected);
			AppendDelta(_endPoint.TotalGarbageCollectionTime, ref _lastGcTime, _gcTime);

			if (Log.IsDebugEnabled)
			{
				var builder = CreateReport();
				Log.Debug(builder);
			}
		}

		[Pure]
		internal string CreateReport()
		{
			var builder = new StringBuilder();
			builder.AppendFormat("{0} Statistics Report", _endPoint.Name);
			builder.AppendLine();
			builder.AppendLine("Network In:");
			builder.AppendFormat("  {0:F1} Kb/s",
			                     _bytesReceived.Average / 1024);
			builder.AppendLine();
			builder.AppendFormat("  {0:F1} messages/s",
			                     _messagesReceived.Average);
			builder.AppendLine();
			builder.AppendFormat("  avg. size: {0:F1}Kb",
			                     _bytesReceived.Average / _messagesReceived.Average / 1024);
			builder.AppendLine();

			builder.AppendLine("Network Out:");
			builder.AppendFormat("  {0:F1} Kb/s",
			                     _bytesSent.Average / 1024);
			builder.AppendLine();
			builder.AppendFormat("  {0:F1} messages/s",
			                     _messagesSent.Average);
			builder.AppendLine();
			builder.AppendFormat("  avg. size: {0:F1}Kb",
			                     _bytesSent.Average / _messagesSent.Average / 1024);

			builder.AppendLine();
			builder.AppendLine("RPC:");
			builder.AppendFormat("  Pending method calls: {0}", _endPoint.NumPendingMethodCalls);
			builder.AppendLine();
			builder.AppendFormat("  Pending method invocations: {0}", _endPoint.NumPendingMethodInvocations);
			var rtt = _endPoint.AverageRoundTripTime;
			if (rtt != null)
			{
				builder.AppendLine();
				builder.AppendFormat("  avg. latency: {0:F1}ms", rtt.Value.TotalMilliseconds);
			}

			builder.AppendLine();
			builder.AppendLine("Memory:");
			builder.AppendFormat("  avg. GC: {0:F2}%", _gcTime.Average.TotalMilliseconds / 10);
			builder.AppendLine();
			builder.AppendFormat("  Servants collected: {0:F1}/s", _servantsCollected.Average);
			builder.AppendLine();
			builder.AppendFormat("  Proxies collected: {0:F1}/s", _proxiesCollected.Average);
			return builder.ToString();
		}

		private void AppendDelta(long currentValue, ref long previousValue, StatisticsContainer container)
		{
			var delta = currentValue - previousValue;
			previousValue = currentValue;
			container.Enqueue(delta);
		}

		private void AppendDelta(TimeSpan currentValue, ref TimeSpan previousValue, TimeSpanStatisticsContainer container)
		{
			var delta = currentValue - previousValue;
			previousValue = currentValue;
			container.Enqueue(delta);
		}
	}
}