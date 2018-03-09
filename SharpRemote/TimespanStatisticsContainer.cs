using System;

namespace SharpRemote
{
	/// <summary>
	///     Collects statistics about a series of timespan measurements.
	/// </summary>
	internal sealed class TimespanStatisticsContainer
	{
		private readonly StatisticsContainer _ticks;

		public TimespanStatisticsContainer(int numSamples)
		{
			_ticks = new StatisticsContainer(numSamples);
		}

		/// <summary>
		/// </summary>
		public TimeSpan Average => TimeSpan.FromTicks((long) _ticks.Average);

		/// <summary>
		///     Adds a sample to this container.
		/// </summary>
		/// <param name="value"></param>
		public void Enqueue(TimeSpan value)
		{
			_ticks.Enqueue(value.Ticks);
		}
	}
}