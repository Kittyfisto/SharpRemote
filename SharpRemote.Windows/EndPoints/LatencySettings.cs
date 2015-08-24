using System;

// ReSharper disable CheckNamespace
namespace SharpRemote
// ReSharper restore CheckNamespace
{
	/// <summary>
	/// This class is used to describe if and how latency measurements are performed.
	/// </summary>
	public class LatencySettings
	{
		/// <summary>
		/// The interval at which latency measurements are performed.
		/// </summary>
		/// <remarks>
		/// Settings this to a very low (a few milliseconds) value might have a negative impact on performance.
		/// </remarks>
		/// <remarks>
		/// The default value is 100ms.
		/// </remarks>
		/// <remarks>
		/// Settings this to zero disables all latency measurements.
		/// </remarks>
		public TimeSpan Interval;

		/// <summary>
		/// The amount of samples over which the average roundtrip time is calulated.
		/// </summary>
		/// <remarks>
		/// The default value is 10.
		/// </remarks>
		public int NumSamples;

		/// <summary>
		/// Initializes a new instance of this class with default values.
		/// </summary>
		public LatencySettings()
		{
			Interval = TimeSpan.FromMilliseconds(100);
			NumSamples = 10;
		}
	}
}