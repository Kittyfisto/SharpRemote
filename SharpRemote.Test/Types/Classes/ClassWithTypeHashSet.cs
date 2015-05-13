using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class ClassWithTypeHashSet : IEquatable<ClassWithTypeHashSet>
	{
		[DataMember]
		public HashSet<Type> Values { get; set; }

		public bool Equals(ClassWithTypeHashSet other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			if (ReferenceEquals(Values, other.Values))
				return true;
			if (Values == null && other.Values != null ||
			    Values != null && other.Values == null)
				return false;

			return Values.All(other.Values.Contains);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is ClassWithTypeHashSet && Equals((ClassWithTypeHashSet) obj);
		}

		public override int GetHashCode()
		{
			return (Values != null ? Values.GetHashCode() : 0);
		}

		public static bool operator ==(ClassWithTypeHashSet left, ClassWithTypeHashSet right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(ClassWithTypeHashSet left, ClassWithTypeHashSet right)
		{
			return !Equals(left, right);
		}
	}
}