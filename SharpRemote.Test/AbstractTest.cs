using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;
using log4net.Core;

namespace SharpRemote.Test
{
	public abstract class AbstractTest
	{
		[OneTimeSetUp]
		public virtual void TestFixtureSetUp()
		{
			TestLogger.EnableConsoleLogging(Level.Error);
			var loggers = Loggers;
			if (loggers != null)
			{
				foreach (var pair in loggers)
				{
					TestLogger.SetLevel(pair.Type, pair.Level);
				}
			}
		}

		/// <summary>
		/// The loggers that shall be enabled for this test and write to the console.
		/// </summary>
		public virtual LogItem[] Loggers { get { return null; } }

		[SetUp]
		public void SetUp()
		{
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			TestLogger.DisableConsoleLogging();
		}

		public static bool WaitFor(Func<bool> fn, TimeSpan timeout)
		{
			DateTime start = DateTime.Now;
			DateTime now = start;
			while ((now - start) < timeout)
			{
				if (fn())
					return true;

				Thread.Sleep(TimeSpan.FromMilliseconds(10));

				now = DateTime.Now;
			}

			return false;
		}
	}
}