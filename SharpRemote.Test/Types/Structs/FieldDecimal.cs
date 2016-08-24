using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldDecimal : IEquatable<FieldDecimal>
	{
		[DataMember] public decimal Value;

		public bool Equals(FieldDecimal other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is FieldDecimal && Equals((FieldDecimal) obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(FieldDecimal left, FieldDecimal right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FieldDecimal left, FieldDecimal right)
		{
			return !left.Equals(right);
		}
	}
}