using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct NullableFieldStruct : IEquatable<NullableFieldStruct>
	{
		[DataMember] public int? Value;

		public bool Equals(NullableFieldStruct other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is NullableFieldStruct && Equals((NullableFieldStruct) obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(NullableFieldStruct left, NullableFieldStruct right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(NullableFieldStruct left, NullableFieldStruct right)
		{
			return !left.Equals(right);
		}
	}
}