using System;
using System.IO;
using System.Threading;
using SharpRemote.EndPoints;

namespace SharpRemote
{
	/// <summary>
	///     Represents a method call that has yet to be completed.
	/// </summary>
	/// <remarks>
	///     Helps to reduce the amount of resources (handles, streams) that are created and destroyed over and
	///     over in order to reduce the amount of pressure on the GC as well as to reduce the amount of native
	///     resources that are created and destroyed.
	/// </remarks>
	internal sealed class PendingMethodCall
		: IDisposable
	{
		private readonly MemoryStream _message;
		private readonly ManualResetEvent _waitHandle;
		private readonly BinaryWriter _writer;
		private Action<PendingMethodCall> _callback;
		private int _messageLength;

		private MessageType _messageType;
		private BinaryReader _reader;
		private long _rpcId;

		public PendingMethodCall()
		{
			_waitHandle = new ManualResetEvent(false);
			_message = new MemoryStream();
			_writer = new BinaryWriter(_message);
		}

		public long RpcId
		{
			get { return _rpcId; }
		}

		public BinaryReader Reader
		{
			get { return _reader; }
		}

		public MessageType MessageType
		{
			get { return _messageType; }
		}

		public long MessageLength
		{
			get { return _messageLength; }
		}

		public void Dispose()
		{
			_waitHandle.Dispose();
			_writer.Dispose();
			_message.Dispose();
		}

		public override string ToString()
		{
			return string.Format("RPC #{0}, {1} bytes",
			                     _rpcId, _messageLength);
		}

		public byte[] GetMessage(out int length)
		{
			length = _messageLength;
			return _message.GetBuffer();
		}

		public void HandleResponse(MessageType messageType, BinaryReader reader)
		{
			_messageType = messageType;
			_reader = reader;
			_waitHandle.Set();

			var fn = Interlocked.Exchange(ref _callback, value: null);
			fn?.Invoke(this);
		}

		public void Reset(ulong servantId,
		                  string interfaceType,
		                  string methodName,
		                  MemoryStream arguments,
		                  long rpcId,
		                  Action<PendingMethodCall> callback)
		{
			// The first 4 bytes of the message shall contain its length which we only
			// know after writing the message, hence we offset the stream by 4 bytes first
			_message.Position = 4;
			_writer.Write(rpcId);
			_writer.Write((byte) MessageType.Call);
			_writer.Write(servantId);
			_writer.Write(interfaceType);
			_writer.Write(methodName);

			if (arguments != null)
			{
				byte[] data = arguments.GetBuffer();
				var dataLength = (int) arguments.Length;
				_writer.Write(data, 0, dataLength);
			}

			_writer.Flush();

			// And then write the payload length into the first 4 bytes
			_messageLength = (int) _message.Position;
			int payloadSize = _messageLength - 4;
			_message.Position = 0;
			_writer.Write(payloadSize);

			_rpcId = rpcId;
			_waitHandle.Reset();
			_messageType = MessageType.None;
			_callback = callback;
			_reader = null;
		}

		public void Wait()
		{
			if (!_waitHandle.WaitOne())
				throw new NotImplementedException();
		}
	}
}