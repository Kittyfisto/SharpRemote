using System;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace SharpRemote.Test
{
	internal static class TestLogger
	{
		#region Static Methods

		public static void EnableConsoleLogging()
		{
			EnableConsoleLogging(Level.All);
		}

		public static void EnableConsoleLogging(Level level)
		{
			var hierarchy = (Hierarchy) LogManager.GetRepository();
			var appender = new ConsoleAppender
				{
					Layout = new PatternLayout("%date %-20message %n"),
				};
			hierarchy.Root.AddAppender(appender);
			hierarchy.Root.Level = level;
			hierarchy.Configured = true;
		}

		public static void DisableConsoleLogging()
		{
			var hierarchy = (Hierarchy)LogManager.GetRepository();
			hierarchy.Clear();
		}

		public static void SetLevel(Type type, Level level)
		{
			var hierarchy = (Hierarchy)LogManager.GetRepository();
			var logger = (Logger)hierarchy.GetLogger(type.FullName);
			logger.Level = level;
		}

		public static void SetLevel<T>(Level level)
		{
			SetLevel(typeof(T), level);
		}

		#endregion
	}
}