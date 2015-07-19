using System;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace SharpRemote
{
	/// <summary>
	/// Log4net appender that delegates all log events to a user specified action.
	/// </summary>
	public sealed class LogInterceptor
		: AppenderSkeleton
		  , IDisposable
	{
		private readonly Action<LoggingEvent> _logAction;
		private IAppenderAttachable _root;

		public LogInterceptor(Action<LoggingEvent> logAction)
			: this(logAction, Level.All)
		{
			
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="logAction"></param>
		/// <param name="level"></param>
		public LogInterceptor(Action<LoggingEvent> logAction, Level level)
		{
			Threshold = level;

			_logAction = logAction;
			_root = ((Hierarchy) LogManager.GetRepository()).Root;
			_root.AddAppender(this);
		}

		public void Dispose()
		{
			if (_root != null)
			{
				_root.RemoveAppender(this);
				_root = null;
			}
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			_logAction(loggingEvent);
		}
	}
}