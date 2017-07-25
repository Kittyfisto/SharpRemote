using System;
using System.Runtime.Serialization;

namespace SharpRemote.Watchdog
{
	/// <summary>
	///     Describes a specific version of an application.
	///     Used to start an installation / upgrade or to (partially) describe an installed application.
	/// </summary>
	[DataContract]
	public struct ApplicationDescriptor : IEquatable<ApplicationDescriptor>
	{
		/// <summary>
		///     The name of the application - must be unique.
		/// </summary>
		[DataMember]
		public string Name { get; set; }

		/// <summary>
		///     Tests if this and the given descriptor are equal.
		///     Two descriptors are equal if they have the same names (case sensitive).
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(ApplicationDescriptor other)
		{
			return string.Equals(Name, other.Name);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return string.Format("{0}", Name);
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is ApplicationDescriptor && Equals((ApplicationDescriptor) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				return ((Name != null ? Name.GetHashCode() : 0)*397);
			}
		}

		/// <summary>
		///     Compares two descriptors for equality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator ==(ApplicationDescriptor left, ApplicationDescriptor right)
		{
			return left.Equals(right);
		}

		/// <summary>
		///     Compares two descriptors for inequality.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static bool operator !=(ApplicationDescriptor left, ApplicationDescriptor right)
		{
			return !left.Equals(right);
		}
	}
}