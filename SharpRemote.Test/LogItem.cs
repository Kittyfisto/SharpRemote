using System;
using log4net.Core;

namespace SharpRemote.Test
{
	public struct LogItem
	{
		public Type Type;
		public Level Level;

		public LogItem(Type type)
		{
			Type = type;
			Level = Level.Info;
		}

		public LogItem(Type type, Level level)
		{
			Type = type;
			Level = level;
		}
	}
}