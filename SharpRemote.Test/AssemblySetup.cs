﻿using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace SharpRemote.Test
{
	[SetUpFixture]
	public sealed class AssemblySetup
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			string codeBase = Assembly.GetExecutingAssembly().Location;
			UriBuilder uri = new UriBuilder(codeBase);
			string path = Uri.UnescapeDataString(uri.Path);
			var directory = Path.GetDirectoryName(path);

			Directory.SetCurrentDirectory(directory);
		}
	}
}