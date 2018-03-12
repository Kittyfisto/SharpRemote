﻿using System.ComponentModel;

namespace SharpRemote
{
	/// <summary>
	/// </summary>
	public sealed class GrainIdGenerator
	{
		/// <summary>
		///     The maximum possible <see cref="IGrain.ObjectId" /> which can be generated by this object.
		/// </summary>
		public const ulong MaxValue = ulong.MaxValue;

		/// <summary>
		///     The minimum <see cref="IGrain.ObjectId" /> which can be generated by this object.
		/// </summary>
		public const ulong MinValue = (ulong.MaxValue - ulong.MinValue) / 2;

		/// <summary>
		///     The range of values that is reserved by this generator.
		///     Spans 2^63 continuous values from 2^63-1 to 2^64-1.
		///     This leaves 0 to 2^63-2 for user-defined ids.
		/// </summary>
		public static readonly GrainIdRange TotalReservedRange;

		private readonly GrainIdRange _range;
		private ulong _nextId;
		private bool _rangeExhausted;

		static GrainIdGenerator()
		{
			TotalReservedRange = new GrainIdRange(MinValue, MaxValue);
		}

		/// <summary>
		///     Initializes this generator for the given endpoint type.
		/// </summary>
		/// <param name="type"></param>
		public GrainIdGenerator(EndPointType type)
		{
			const ulong midPoint = (MaxValue - MinValue) / 2 + MinValue;

			switch (type)
			{
				case EndPointType.Client:
					_range = new GrainIdRange(MinValue, midPoint - 1);
					break;

				case EndPointType.Server:
					_range = new GrainIdRange(midPoint, MaxValue);
					break;

				default:
					throw new InvalidEnumArgumentException(nameof(type), (int) type, typeof(EndPointType));
			}

			_nextId = _range.Minimum;
		}

		/// <summary>
		///     Generates an id for the next grain.
		///     For the same <see cref="GrainIdGenerator"/> instance, this method will never generate the same value twice.
		/// </summary>
		/// <returns></returns>
		public ulong GetGrainId()
		{
			if (_rangeExhausted)
				throw new GrainIdRangeExhaustedException();

			if (_nextId == _range.Maximum)
			{
				_rangeExhausted = true;
				throw new GrainIdRangeExhaustedException();
			}

			var id = _nextId++;
			return id;
		}
	}
}