using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldObjectStruct
	{
		[DataMember] public object Value;

		#region Public Methods

		public bool Equals(FieldObjectStruct other)
		{
			return Equals(Value, other.Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is FieldObjectStruct && Equals((FieldObjectStruct) obj);
		}

		public override int GetHashCode()
		{
			return (Value != null ? Value.GetHashCode() : 0);
		}

		#endregion
	}
}