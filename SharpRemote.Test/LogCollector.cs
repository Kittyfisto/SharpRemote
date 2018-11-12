using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace SharpRemote.Test
{
	public sealed class LogCollector
		: AppenderSkeleton
		, IDisposable
	{
		private readonly object _syncRoot;
		private readonly string _namespace;
		private readonly List<LoggingEvent> _events;
		private readonly HashSet<Level> _levels;

		public LogCollector(string @namespace, params Level[] levels)
		{
			_syncRoot = new object();
			_namespace = @namespace;
			_levels = new HashSet<Level>(levels);
			_events = new List<LoggingEvent>();

			Hierarchy h = (Hierarchy) LogManager.GetRepository();
			h.Root.AddAppender(this);

			if (_levels.Contains(Level.All))
			{
				_levels.Add(Level.Debug);
				_levels.Add(Level.Info);
				_levels.Add(Level.Warn);
				_levels.Add(Level.Error);
				_levels.Add(Level.Fatal);
			}

			if (levels.Any())
			{
				var min = levels.Min();
				h.Root.Level = min;
			}

			h.Configured = true;
		}

		public void Dispose()
		{
			Hierarchy h = (Hierarchy) LogManager.GetRepository();
			h.Root.RemoveAppender(this);
		}

		public IReadOnlyList<LoggingEvent> Events
		{
			get
			{
				lock (_syncRoot)
				{
					return _events.ToList();
				}
			}
		}

		public string Log
		{
			get
			{
				var buffer = new StringBuilder();
				foreach (var @event in _events)
				{
					buffer.AppendLine(@event.RenderedMessage);
				}

				return buffer.ToString();
			}
		}

		public void PrintAll()
		{
			Console.WriteLine(Log);
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			if (!loggingEvent.LoggerName.StartsWith(_namespace))
				return;

			if (!_levels.Contains(loggingEvent.Level))
				return;

			lock (_syncRoot)
			{
				_events.Add(loggingEvent);
			}
		}
	}
}
