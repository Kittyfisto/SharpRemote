using NUnit.Framework;

namespace SharpRemote.Test
{
	/// <summary>
	///     Attribute to mark performance tests.
	///     These usually only run on a limited set of hardware (due to measuring timings) and thus are ignored on the build server.
	/// </summary>
	public sealed class PerformanceTestAttribute
		: CategoryAttribute
	{
		public PerformanceTestAttribute()
			: base("PerformanceTest")
		{
		}
	}
}