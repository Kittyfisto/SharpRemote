using System.Diagnostics.Tracing;

namespace SharpRemote.ETW
{
	/// <summary>
	///     This class is used to track anything related to methods that are waiting to be
	///     * sent to
	///     * executed on
	///     * or answered by
	///     the other endpoint.
	/// </summary>
	[EventSource(Name = "SharpRemote.PendingMethods")]
	public sealed class PendingMethodsEventSource
		: EventSource
	{
		private const int RpcEnqueuedId = 1;
		private const int RpcDequeuedId = 2;
		private const int QueueCountChangedId = 3;

		public static readonly PendingMethodsEventSource Instance;

		static PendingMethodsEventSource()
		{
			Instance = new PendingMethodsEventSource();
		}

		private PendingMethodsEventSource()
		{
		}

		[Event(RpcEnqueuedId, Message = "RPC #{0} {1}.{2} enqueued (argument length: {3} bytes)", Level = EventLevel.Verbose)]
		internal void Enqueued(long id,
		                       string interfaceType,
		                       string methodName,
		                       long argumentLengthInBytes)
		{
			if (IsEnabled())
				WriteEvent(RpcEnqueuedId, id, interfaceType, methodName, argumentLengthInBytes);
		}

		[Event(RpcDequeuedId, Message = "RPC #{0} dequeued", Level = EventLevel.Verbose)]
		internal void Dequeued(long id)
		{
			if (IsEnabled())
				WriteEvent(RpcDequeuedId, id);
		}

		[Event(QueueCountChangedId, Message = "Queue count changed: {0}", Level = EventLevel.LogAlways)]
		internal void QueueCountChanged(int count)
		{
			if (IsEnabled())
				WriteEvent(QueueCountChangedId, count);
		}
	}
}