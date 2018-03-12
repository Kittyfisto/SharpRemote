using System;

namespace SharpRemote
{
	/// <summary>
	///     Collects statistics about a series of timespan measurements.
	/// </summary>
	internal sealed class TimeSpanStatisticsContainer
	{
		private readonly StatisticsContainer _ticks;

		public TimeSpanStatisticsContainer(int numSamples)
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

		public override string ToString()
		{
			return string.Format("{0} samples, avg: {1}", _ticks.Count, Average);
		}
	}
}