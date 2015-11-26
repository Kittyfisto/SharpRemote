using System;
using System.Collections.Generic;
using System.IO;

namespace SharpRemote
{
	internal sealed class PendingMethodsQueue2
		: IDisposable
	{
		private readonly int _capacity;
		private readonly Stack<PendingMethodCall> _buffer;
		private readonly PriorityQueue<>
		private bool _isDisposed;

		public PendingMethodsQueue2(int capacity)
		{
			_capacity = capacity;
			_buffer = new Stack<PendingMethodCall>(capacity);
			for (int i = 0; i < capacity; ++i)
			{
				_buffer.Push(new PendingMethodCall());
			}
		}

		public PendingMethodCall Enqueue(ulong servantId,
		                                 string interfaceType,
		                                 string methodName,
		                                 MemoryStream arguments,
		                                 long rpcId,
		                                 Action<PendingMethodCall> callback = null)
		{
			var call = _buffer.Pop();
			call.Reset(servantId, interfaceType, methodName, arguments, rpcId, callback);
			
		}

		public bool IsDisposed
		{
			get { return _isDisposed; }
		}

		public void Dispose()
		{
			_isDisposed = true;
		}
	}
}