using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class ClassWithNullableStructProperty : IEquatable<ClassWithNullableStructProperty>
	{
		[DataMember(Order = 1)]
		public float? Value { get; set; }

		public bool Equals(ClassWithNullableStructProperty other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ClassWithNullableStructProperty && Equals((ClassWithNullableStructProperty) obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public static bool operator ==(ClassWithNullableStructProperty left, ClassWithNullableStructProperty right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ClassWithNullableStructProperty left, ClassWithNullableStructProperty right)
		{
			return !Equals(left, right);
		}
	}
}