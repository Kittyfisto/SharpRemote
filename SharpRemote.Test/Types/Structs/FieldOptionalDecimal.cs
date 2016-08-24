using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldOptionalDecimal : IEquatable<FieldOptionalDecimal>
	{
		[DataMember] public decimal? Value;

		public bool Equals(FieldOptionalDecimal other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is FieldOptionalDecimal && Equals((FieldOptionalDecimal) obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(FieldOptionalDecimal left, FieldOptionalDecimal right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FieldOptionalDecimal left, FieldOptionalDecimal right)
		{
			return !left.Equals(right);
		}
	}
}