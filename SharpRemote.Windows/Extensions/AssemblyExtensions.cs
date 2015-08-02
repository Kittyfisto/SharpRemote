using System;
using System.IO;
using System.Reflection;

namespace SharpRemote.Extensions
{
	internal static class AssemblyExtensions
	{
		public static string GetDirectory(this Assembly assembly)
		{
			string codeBase = assembly.CodeBase;
			var uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			return Path.GetDirectoryName(path);
		}
	}
}
