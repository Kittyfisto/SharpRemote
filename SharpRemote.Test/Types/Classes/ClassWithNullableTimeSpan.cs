using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public class ClassWithNullableTimeSpan : IEquatable<ClassWithNullableTimeSpan>
	{
		[DataMember] public TimeSpan? Value;

		public bool Equals(ClassWithNullableTimeSpan other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((ClassWithNullableTimeSpan) obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}
}