using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldString : IEquatable<FieldString>
	{
		[DataMember]
		public string Value;

		public bool Equals(FieldString other)
		{
			return Value == other.Value;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is FieldString && Equals((FieldString)obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(FieldString left, FieldString right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(FieldString left, FieldString right)
		{
			return !left.Equals(right);
		}
	}
}