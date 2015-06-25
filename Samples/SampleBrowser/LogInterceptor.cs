using System;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Repository.Hierarchy;

namespace SampleBrowser
{
	internal sealed class LogInterceptor
		: AppenderSkeleton
		  , IDisposable
	{
		private readonly Action<string> _logAction;
		private IAppenderAttachable _root;

		public LogInterceptor(Action<string> logAction)
		{
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
			_logAction(loggingEvent.RenderedMessage);
		}
	}
}