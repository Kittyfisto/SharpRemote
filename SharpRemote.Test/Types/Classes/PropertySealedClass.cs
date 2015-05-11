using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class PropertySealedClass
	{
		[DataMember]
		public string Value1 { get; set; }

		[DataMember]
		public int Value2 { get; set; }

		[DataMember]
		public double Value3 { get; set; }

		#region Public Methods

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is PropertySealedClass && Equals((PropertySealedClass) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (Value1 != null ? Value1.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ Value2;
				hashCode = (hashCode * 397) ^ Value3.GetHashCode();
				return hashCode;
			}
		}

		#endregion

		private bool Equals(PropertySealedClass other)
		{
			return string.Equals(Value1, other.Value1) && Value2 == other.Value2 && Value3.Equals(other.Value3);
		}
	}
}