using System;
using System.Collections.Generic;
using SharpRemote.Test.Types.Interfaces;

namespace SharpRemote.Test.Types.Classes
{
	public sealed class Processor
		: IProcessor
	{
		private readonly List<IListener> _listeners;

		public Processor()
		{
			_listeners = new List<IListener>();
		}

		public List<IListener> Listeners
		{
			get { return _listeners; }
		}

		public void Process()
		{
			Report("Starting...");
			Report("Ending...");
			Report("Success...");
		}

		public void Report(string message)
		{
			foreach (var listener in _listeners)
			{
				listener.Report(message);
			}
		}

		public void AddListener(IListener listener)
		{
			_listeners.Add(listener);
		}

		public void RemoveListener(IListener listener)
		{
			_listeners.Remove(listener);
		}
	}
}