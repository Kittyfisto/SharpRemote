using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldUInt32 : IEquatable<FieldUInt32>
	{
		[DataMember]
		public uint Value;

		public bool Equals(FieldUInt32 other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is FieldUInt32 && Equals((FieldUInt32)obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(FieldUInt32 left, FieldUInt32 right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FieldUInt32 left, FieldUInt32 right)
		{
			return !left.Equals(right);
		}
	}
}