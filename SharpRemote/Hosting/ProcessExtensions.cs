using System;
using System.ComponentModel;
using System.Diagnostics;

namespace SharpRemote.Hosting
{
	public static class ProcessExtensions
	{
		public static void TryKill(this Process that)
		{
			try
			{
				that.Kill();
			}
			catch (Win32Exception)
			{
				
			}
			catch (InvalidOperationException)
			{
				
			}
		}
	}
}