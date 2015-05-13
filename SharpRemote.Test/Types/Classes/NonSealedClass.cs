using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public class NonSealedClass : IEquatable<NonSealedClass>
	{
		[DataMember] public bool Value2;

		[DataMember]
		public string Value1 { get; set; }

		public bool Equals(NonSealedClass other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return string.Equals(Value1, other.Value1) && Value2.Equals(other.Value2);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((NonSealedClass) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Value1 != null ? Value1.GetHashCode() : 0)*397) ^ Value2.GetHashCode();
			}
		}

		public static bool operator ==(NonSealedClass left, NonSealedClass right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(NonSealedClass left, NonSealedClass right)
		{
			return !Equals(left, right);
		}
	}
}