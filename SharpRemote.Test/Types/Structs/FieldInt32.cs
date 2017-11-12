using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldInt32 : IEquatable<FieldInt32>
	{
		[DataMember]
		public int Value;

		public bool Equals(FieldInt32 other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is FieldInt32 && Equals((FieldInt32)obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(FieldInt32 left, FieldInt32 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FieldInt32 left, FieldInt32 right)
		{
			return !left.Equals(right);
		}
	}
}