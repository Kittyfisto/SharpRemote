using System.Runtime.Serialization;
using SharpRemote.Test.Types.Classes;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct NestedFieldStruct
	{
		[DataMember]
		public PropertySealedClass N1;

		[DataMember]
		public FieldStruct N2;

		#region Public Methods

		public bool Equals(NestedFieldStruct other)
		{
			return Equals(N1, other.N1) && N2.Equals(other.N2);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is NestedFieldStruct && Equals((NestedFieldStruct) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((N1 != null ? N1.GetHashCode() : 0) * 397) ^ N2.GetHashCode();
			}
		}

		#endregion
	}
}