using System;
using System.ComponentModel;
using System.Diagnostics;

namespace SharpRemote.Extensions
{
	internal static class ProcessExtensions
	{
		public static void TryKill(this Process that)
		{
			try
			{
				if (that != null && !that.HasExited)
				{
					that.Kill();
				}
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