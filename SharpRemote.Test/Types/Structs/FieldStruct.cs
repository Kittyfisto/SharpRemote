using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldStruct : IEquatable<FieldStruct>
	{
		[DataMember] public double A;

		[DataMember] public int B;

		[DataMember] public string C;

		public bool Equals(FieldStruct other)
		{
			return A.Equals(other.A) && B == other.B && string.Equals(C, other.C);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is FieldStruct && Equals((FieldStruct) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
// ReSharper disable NonReadonlyFieldInGetHashCode
				int hashCode = A.GetHashCode();
				hashCode = (hashCode*397) ^ B;
				hashCode = (hashCode * 397) ^ (C != null ? C.GetHashCode() : 0);
// ReSharper restore NonReadonlyFieldInGetHashCode
				return hashCode;
			}
		}

		public static bool operator ==(FieldStruct left, FieldStruct right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FieldStruct left, FieldStruct right)
		{
			return !left.Equals(right);
		}
	}
}