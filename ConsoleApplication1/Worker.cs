using System.Collections.Generic;
using System.Threading;

namespace ConsoleApplication1
{
	public sealed class Worker
		: IWorker
	{
		private readonly List<IDataListener> _listeners;
		private Thread _thread;
		private volatile bool _stopped;

		public Worker()
		{
			_listeners = new List<IDataListener>();
		}

		public void RegisterListener(IDataListener listener)
		{
			_listeners.Add(listener);
		}

		public void Start()
		{
			_thread = new Thread(DoWork);
			_thread.Start();
		}

		private void DoWork()
		{
			long i = 0;
			while (!_stopped)
			{
				foreach (var listener in _listeners)
				{
					listener.Process(i);
				}

				++i;
			}
		}

		public void Stop()
		{
			_stopped = true;
			var thread = _thread;
			if (thread != null)
			{
				thread.Join(100);
			}
		}
	}
}