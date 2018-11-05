using System;
using System.Collections.Generic;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace SharpRemote.Test
{
	sealed class LogCollector
		: AppenderSkeleton
		, IDisposable
	{
		private readonly string _namespace;
		private readonly List<LoggingEvent> _events;
		private readonly HashSet<Level> _levels;

		public LogCollector(string @namespace, params Level[] levels)
		{
			_namespace = @namespace;
			_levels = new HashSet<Level>(levels);
			_events = new List<LoggingEvent>();

			Hierarchy h = (Hierarchy) LogManager.GetRepository();
			h.Root.AddAppender(this);
			h.Configured = true;
		}

		public void Dispose()
		{
			Hierarchy h = (Hierarchy) LogManager.GetRepository();
			h.Root.RemoveAppender(this);
		}

		public IReadOnlyList<LoggingEvent> Events => _events;

		protected override void Append(LoggingEvent loggingEvent)
		{
			if (!loggingEvent.LoggerName.StartsWith(_namespace))
				return;

			if (!_levels.Contains(loggingEvent.Level))
				return;

			_events.Add(loggingEvent);
		}
	}
}
