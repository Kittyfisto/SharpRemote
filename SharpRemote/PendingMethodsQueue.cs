using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using log4net;
using SharpRemote.EndPoints;
using SharpRemote.ETW;

namespace SharpRemote
{
	/// <summary>
	///     Handles remote method calls that are not completed yet.
	/// </summary>
	internal sealed class PendingMethodsQueue
		: IDisposable
	{
		private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		internal const int MaxNumRecycledCalls = 20;

		private readonly string _endPointName;
		private readonly int _maxConcurrentCalls;
		private readonly Dictionary<long, PendingMethodCall> _pendingCalls;
		private readonly Queue<PendingMethodCall> _recycledMessages;
		private readonly object _syncRoot;

		private bool _isConnected;

		private bool _isDisposed;
		private BlockingQueue<PendingMethodCall> _pendingWrites;

		/// <summary>
		/// </summary>
		/// <param name="endPointName"></param>
		/// <param name="maxConcurrentCalls">The total number of concurrent calls that may be pending at any given time, any further call stalls the calling thread, even if async</param>
		public PendingMethodsQueue(string endPointName = "", int maxConcurrentCalls = 2000)
		{
			if (maxConcurrentCalls < 0)
				throw new ArgumentOutOfRangeException(nameof(maxConcurrentCalls));

			_endPointName = endPointName;
			_maxConcurrentCalls = maxConcurrentCalls;
			_syncRoot = new object();
			_recycledMessages = new Queue<PendingMethodCall>();
			_pendingCalls = new Dictionary<long, PendingMethodCall>();
		}

		internal Queue<PendingMethodCall> RecycledMessages => _recycledMessages;

		internal BlockingQueue<PendingMethodCall> PendingWrites => _pendingWrites;

		/// <summary>
		///     Whether or not this endpoint is currently connected.
		/// </summary>
		/// <remarks>
		///     While set to false, all calls to <see cref="Enqueue" /> throw a
		/// </remarks>
		public bool IsConnected
		{
			get { return _isConnected; }
			set
			{
				lock (_syncRoot)
				{
					_isConnected = value;

					if (_isConnected)
					{
						_pendingWrites = new BlockingQueue<PendingMethodCall>(_maxConcurrentCalls);
					}
				}
			}
		}

		public int NumPendingCalls
		{
			get
			{
				BlockingQueue<PendingMethodCall> pendingWrites = _pendingWrites;
				return pendingWrites != null ? pendingWrites.Count : 0;
			}
		}

		public bool IsDisposed => _isDisposed;

		public void Dispose()
		{
			DisposePendingWrites();

			_isDisposed = true;
		}

		/// <summary>
		///     Retrieves the next message for writing from the queue.
		/// </summary>
		/// <param name="length"></param>
		/// <returns></returns>
		public byte[] TakePendingWrite(out int length)
		{
			BlockingQueue<PendingMethodCall> pendingWrites = _pendingWrites;
			if (pendingWrites == null)
				throw new OperationCanceledException(_endPointName);

			PendingMethodCall message = pendingWrites.Dequeue();
			PendingMethodsEventSource.Instance.Dequeued(_endPointName, message.RpcId);
			PendingMethodsEventSource.Instance.QueueCountChanged(_endPointName, pendingWrites.Count);

			return message.GetMessage(out length);
		}

		/// <summary>
		///     Shall be called once an answer to a pending call has been received.
		///     Causes the <see cref="WaitHandle" /> of the pending call to be set.
		/// </summary>
		/// <param name="rpcId"></param>
		/// <param name="messageType"></param>
		/// <param name="reader"></param>
		public bool HandleResponse(long rpcId, MessageType messageType, BinaryReader reader)
		{
			lock (_syncRoot)
			{
				PendingMethodCall methodCall;
				if (_pendingCalls.TryGetValue(rpcId, out methodCall))
				{
					Log.DebugFormat("Found pending method '#{0}'", rpcId);

					methodCall.HandleResponse(messageType, reader);
					return true;
				}

				Log.DebugFormat("No pending method '#{0}'!", rpcId);
			}

			return false;
		}

		public void CancelAllCalls()
		{
			lock (_syncRoot)
			{
				if (_pendingCalls.Count > 0)
				{
					byte[] exceptionMessage;
					int exceptionLength;

					using (var stream = new MemoryStream())
					using (var writer = new BinaryWriter(stream, Encoding.UTF8))
					{
						AbstractEndPoint.WriteException(writer, new ConnectionLostException());
						exceptionMessage = stream.GetBuffer();
						exceptionLength = (int) stream.Length;
					}

					foreach (PendingMethodCall call in _pendingCalls.Values.ToList())
					{
						var stream = new MemoryStream(exceptionMessage, 0, exceptionLength);
						var reader = new BinaryReader(stream, Encoding.UTF8);
						call.HandleResponse(MessageType.Return | MessageType.Exception, reader);

						PendingMethodsEventSource.Instance.Dequeued(_endPointName, call.RpcId);
						PendingMethodsEventSource.Instance.QueueCountChanged(_endPointName, _pendingCalls.Count);
					}

					var count = _pendingCalls.Count;
					Log.DebugFormat("Removing all '{0}' pending method calls...", count);
					_pendingCalls.Clear();
					Log.DebugFormat("All '{0}' pending method calls removed", count);
				}

				DisposePendingWrites();
			}
		}

		/// <summary>
		///     Shall be called once a pending call has been completely handled.
		/// </summary>
		/// <param name="methodCall"></param>
		public void Recycle(PendingMethodCall methodCall)
		{
			lock (_syncRoot)
			{
				var id = methodCall.RpcId;
				if (Log.IsDebugEnabled)
					Log.DebugFormat("Removing method call '#{0}'...", id);

				_pendingCalls.Remove(id);

				if (Log.IsDebugEnabled)
					Log.DebugFormat("Method call '#{0}' removed", id);

				// We do not want to recycle thousands of messages because that can require
				// lots of memory ("big" messages may take 1Mb of memory or even more).
				// Therefore we limit the amount to a constant value (for now).
				// In the future, we might want to limit the amount of used memory instead?
				if (_recycledMessages.Count < MaxNumRecycledCalls)
				{
					Log.Debug("Recycling method call object...");

					_recycledMessages.Enqueue(methodCall);

					if (Log.IsDebugEnabled)
						Log.DebugFormat("Method call object recycled, {0} objects available for re-use", _recycledMessages.Count);
				}
				else
				{
					Log.DebugFormat("Method call object won't be recycled: We're keeping enough alive already!");
				}
			}
		}

		public PendingMethodCall Enqueue(ulong servantId,
		                                 string interfaceType,
		                                 string methodName,
		                                 MemoryStream arguments,
		                                 long rpcId,
		                                 Action<PendingMethodCall> callback = null)
		{
			PendingMethodCall message;

			lock (_syncRoot)
			{
				if (!IsConnected)
					throw new NotConnectedException(_endPointName);

				message = _recycledMessages.Count > 0
					          ? _recycledMessages.Dequeue()
					          : new PendingMethodCall();
				_pendingCalls.Add(rpcId, message);

				int numPendingRpcs = _pendingCalls.Count;
				PendingMethodsEventSource.Instance.Enqueued(_endPointName, rpcId, interfaceType, methodName,
				                                            arguments != null ? arguments.Length : 0);
				PendingMethodsEventSource.Instance.QueueCountChanged(_endPointName, numPendingRpcs);
			}

			message.Reset(servantId, interfaceType, methodName, arguments, rpcId, callback);

			// _pendingWrites can be null, if immediately after we leave this lock, IsConnected is set to false
			BlockingQueue<PendingMethodCall> pendingWrites = _pendingWrites;
			if (pendingWrites != null)
			{
				pendingWrites.Enqueue(message);
			}

			return message;
		}

		private void DisposePendingWrites()
		{
			BlockingQueue<PendingMethodCall> pendingWrites = _pendingWrites;
			if (pendingWrites != null)
			{
				pendingWrites.Dispose();
				_pendingWrites = null;
			}
		}
	}
}
