using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml.Serialization;

namespace SharpRemote.Watchdog
{
	/// <summary>
	/// Represents a file that has been remotely deployed and installed on a machine.
	/// </summary>
	[DataContract]
	[XmlSerializerFormat]
	public sealed class InstalledFile : IEquatable<InstalledFile>
	{
		/// <summary>
		/// The length of the file in bytes.
		/// </summary>
		[DataMember] [XmlAttribute] public long FileLength;

		/// <summary>
		/// The filename (including extension) relative to the special folder its been placed in.
		/// </summary>
		[DataMember] [XmlAttribute] public string Filename;

		/// <summary>
		/// The folder the file is placed in.
		/// </summary>
		[DataMember] [XmlAttribute] public Environment.SpecialFolder Folder;

		/// <summary>
		/// Unique id of the file.
		/// </summary>
		[DataMember] [XmlAttribute] public long Id;

		/// <inheritdoc />
		public bool Equals(InstalledFile other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Id == other.Id && string.Equals(Filename, other.Filename) && Folder == other.Folder &&
			       FileLength == other.FileLength;
		}

		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is InstalledFile && Equals((InstalledFile) obj);
		}

		/// <inheritdoc />
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = Id.GetHashCode();
				hashCode = (hashCode*397) ^ (Filename != null ? Filename.GetHashCode() : 0);
				hashCode = (hashCode*397) ^ (int) Folder;
				hashCode = (hashCode*397) ^ FileLength.GetHashCode();
				return hashCode;
			}
		}
	}
}