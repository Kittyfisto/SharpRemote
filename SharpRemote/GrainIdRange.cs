using System;

namespace SharpRemote
{
	/// <summary>
	///     Describes a range of <see cref="IGrain.ObjectId" />s.
	/// </summary>
	public struct GrainIdRange
	{
		/// <summary>
		///     The minimum possible grain id in this range.
		/// </summary>
		public readonly ulong Minimum;

		/// <summary>
		///     The maximum possible grain id in this range.
		/// </summary>
		public readonly ulong Maximum;

		/// <summary>
		///     Initializes this object.
		/// </summary>
		/// <param name="minimum"></param>
		/// <param name="maximum"></param>
		public GrainIdRange(ulong minimum, ulong maximum)
		{
			if (minimum > maximum)
				throw new ArgumentOutOfRangeException();

			Minimum = minimum;
			Maximum = maximum;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("[{0}, {1}]", Minimum, Maximum);
		}
	}
}