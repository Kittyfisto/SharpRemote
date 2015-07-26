using System.ComponentModel;
using SharpRemote.Exceptions;

namespace SharpRemote
{
	internal sealed class GrainIdGenerator
	{
		private readonly GrainIdRange _range;
		private ulong _nextId;
		private bool _rangeExhausted;

		public const ulong MaxValue = ulong.MaxValue;
		public const ulong MinValue = (ulong.MaxValue - ulong.MinValue) / 2;

		/// <summary>
		///     The range of values that is reserved by this generator.
		///     Spans 2^63 continuous values from 2^63-1 to 2^64-1.
		///     This leaves 0 to 2^63-2 for user-defined ids.
		/// </summary>
		public static readonly GrainIdRange TotalReservedRange;

		static GrainIdGenerator()
		{
			TotalReservedRange = new GrainIdRange(MinValue, MaxValue);
		}

		public GrainIdGenerator(EndPointType type)
		{
			const ulong midPoint = (MaxValue - MinValue)/2 + MinValue;

			switch (type)
			{
				case EndPointType.Client:
					_range = new GrainIdRange(MinValue, midPoint - 1);
					break;

				case EndPointType.Server:
					_range = new GrainIdRange(midPoint, MaxValue);
					break;

				default:
					throw new InvalidEnumArgumentException("type", (int) type, typeof (EndPointType));
			}

			_nextId = _range.Minimum;
		}

		public ulong GetGrainId()
		{
			if (_rangeExhausted)
				throw new GrainIdRangeExhaustedException();

			if (_nextId == _range.Maximum)
			{
				_rangeExhausted = true;
				throw new GrainIdRangeExhaustedException();
			}

			ulong id = _nextId++;
			return id;
		}
	}
}