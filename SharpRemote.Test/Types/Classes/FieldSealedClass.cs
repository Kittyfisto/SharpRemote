using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class FieldSealedClass
	{
		[DataMember]
		public double A;

		[DataMember]
		public int B;

		[DataMember]
		public string C;

		#region Public Methods

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is FieldSealedClass && Equals((FieldSealedClass) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = A.GetHashCode();
				hashCode = (hashCode * 397) ^ B;
				hashCode = (hashCode * 397) ^ (C != null ? C.GetHashCode() : 0);
				return hashCode;
			}
		}

		#endregion

		private bool Equals(FieldSealedClass other)
		{
			return A.Equals(other.A) && B == other.B && string.Equals(C, other.C);
		}
	}
}