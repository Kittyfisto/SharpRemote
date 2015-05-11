using System;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct PropertyStruct
		: IEquatable<PropertyStruct>
	{
		[DataMember]
		public string Value { get; set; }

		#region Public Methods

		public bool Equals(PropertyStruct other)
		{
			return string.Equals(Value, other.Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is PropertyStruct && Equals((PropertyStruct) obj);
		}

		public override int GetHashCode()
		{
			return (Value != null ? Value.GetHashCode() : 0);
		}

		#endregion
	}
}