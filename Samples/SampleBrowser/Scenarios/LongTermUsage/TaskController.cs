using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	public class TaskController
		: ITaskController
		  , IDisposable
	{
		private Thread _dataThread;
		private readonly List<IDataListener> _listeners;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly int? _numDataPackets;
		private int _sequenceNumber;

		public TaskController(int? numDataPackets)
		{
			_listeners = new List<IDataListener>();
			_cancellationTokenSource = new CancellationTokenSource();
			_numDataPackets = numDataPackets;
		}

		private void Do()
		{
			var token = _cancellationTokenSource.Token;
			if (_numDataPackets != null)
			{
				var num = _numDataPackets.Value;
				for (int i = 0; i < num && !token.IsCancellationRequested; ++i)
				{
					var data = CreateData();
					DispatchData(data);
				}
			}
			else
			{
				while (!token.IsCancellationRequested)
				{
					var data = CreateData();
					DispatchData(data);
				}
			}
		}

		private void DispatchData(object data)
		{
			lock (_listeners)
			{
				foreach (var listener in _listeners)
				{
					listener.Process(data);
				}
			}
		}

		private object CreateData()
		{
			return new DataPacket
				{
					SequenceNumber = ++_sequenceNumber
				};
		}

		public Task ExecuteCommand(ICommandDescription command)
		{
			return null;
		}

		public void Start()
		{
			_dataThread = new Thread(Do);
		}

		public bool IsRunning { get { return _dataThread != null; } }

		public void Stop()
		{
			_cancellationTokenSource.Cancel();
			_dataThread.Join();
			_dataThread = null;
		}

		public void RegisterDataListener(IDataListener listener)
		{
			lock (_listeners)
				_listeners.Add(listener);
		}

		public void UnregisterDataListener(IDataListener listener)
		{
			lock (_listeners)
				_listeners.Remove(listener);
		}

		public void Dispose()
		{
			Stop();
			_cancellationTokenSource.Dispose();
		}
	}
}