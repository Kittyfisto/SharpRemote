using System;

namespace SharpRemote
{
	internal struct GrainIdRange
	{
		public readonly ulong Minimum;
		public readonly ulong Maximum;

		public GrainIdRange(ulong minimum, ulong maximum)
		{
			if (minimum > maximum)
				throw new ArgumentOutOfRangeException();

			Minimum = minimum;
			Maximum = maximum;
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}]", Minimum, Maximum);
		}
	}
}