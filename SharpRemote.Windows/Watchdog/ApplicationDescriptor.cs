using System;
using System.Runtime.Serialization;

namespace SharpRemote.Watchdog
{
	/// <summary>
	///     Describes a specific version of an application.
	/// </summary>
	[DataContract]
	public struct ApplicationDescriptor : IEquatable<ApplicationDescriptor>
	{
		[DataMember]
		public string Name { get; set; }

		public override string ToString()
		{
			return string.Format("{0}", Name);
		}

		public bool Equals(ApplicationDescriptor other)
		{
			return string.Equals(Name, other.Name);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is ApplicationDescriptor && Equals((ApplicationDescriptor) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0)*397);
			}
		}

		public static bool operator ==(ApplicationDescriptor left, ApplicationDescriptor right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ApplicationDescriptor left, ApplicationDescriptor right)
		{
			return !left.Equals(right);
		}
	}
}