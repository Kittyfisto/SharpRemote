using System.Diagnostics.Tracing;

namespace SharpRemote.ETW
{
	/// <summary>
	/// This class is used to track anything related to methods that are waiting to be
	/// * sent to
	/// * executed on
	/// * or answered by
	/// the other endpoint.
	/// </summary>
	[EventSource(Name = "SharpRemote.PendingMethods")]
	public sealed class PendingMethodsEventSource
		: EventSource
	{
		public static readonly PendingMethodsEventSource Instance;

		static PendingMethodsEventSource()
		{
			Instance = new PendingMethodsEventSource();
		}

		private PendingMethodsEventSource()
		{}

		private const int RpcEnqueuedId = 1;
		private const int RpcDequeuedId = 1;

		[Event(RpcEnqueuedId, Message = "RPC #{0} enqueued, {1} total pending", Level = EventLevel.LogAlways)]
		internal void Enqueued(long id, int numPendingRpcs)
		{
			if (IsEnabled())
				WriteEvent(RpcEnqueuedId, id, numPendingRpcs);
		}

		[Event(RpcDequeuedId, Message = "RPC #{0} dequeued, {1} total pending", Level = EventLevel.LogAlways)]
		internal void Dequeued(long id, int numPendingRpcs)
		{
			if (IsEnabled())
				WriteEvent(RpcDequeuedId, id, numPendingRpcs);
		}
	}
}