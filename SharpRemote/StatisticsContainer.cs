namespace SharpRemote
{
	/// <summary>
	///     Collects statistics about a series of measurements.
	/// </summary>
	internal sealed class StatisticsContainer
	{
		private readonly RingBuffer<long> _ringBuffer;
		private double _average;
		private int _count;
		private long _sum;

		/// <summary>
		/// </summary>
		/// <param name="numSamples">The maximum number of samples over which the average is to be calculated.</param>
		public StatisticsContainer(int numSamples)
		{
			_ringBuffer = new RingBuffer<long>(numSamples);
		}

		public int Count => _count;

		/// <summary>
		/// </summary>
		public double Average => _average;

		/// <summary>
		///     Adds a sample to this container.
		/// </summary>
		/// <param name="value"></param>
		public void Enqueue(long value)
		{
			var previous = _ringBuffer.Enqueue(value);
			if (_count < _ringBuffer.Length)
				++_count;
			_sum -= previous;
			_sum += value;
			_average = 1.0 * _sum / _count;
		}

		public override string ToString()
		{
			return string.Format("{0} samples, avg: {1}", _count, _average);
		}
	}
}