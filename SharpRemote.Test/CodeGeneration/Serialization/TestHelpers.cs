namespace SharpRemote.Test.CodeGeneration.Serialization
{
	public static class TestHelpers
	{
		public static string FormatBytes(long bytes)
		{
			const long oneKilobyte = 1024;
			const long oneMegabyte = oneKilobyte*1024;
			if (bytes > oneMegabyte)
				return string.Format("{0:F2} mb", 1.0*bytes/oneMegabyte);

			if (bytes > oneKilobyte)
				return string.Format("{0:F2} kb", 1.0*bytes/oneKilobyte);

			return string.Format("{0} b", bytes);
		}
	}
}