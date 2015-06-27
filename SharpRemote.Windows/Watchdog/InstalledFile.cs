using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml.Serialization;

namespace SharpRemote.Watchdog
{
	[DataContract]
	[XmlSerializerFormat]
	public sealed class InstalledFile : IEquatable<InstalledFile>
	{
		[DataMember] [XmlAttribute] public long FileLength;
		[DataMember] [XmlAttribute] public string Filename;

		[DataMember] [XmlAttribute] public Environment.SpecialFolder Folder;
		[DataMember] [XmlAttribute] public long Id;

		public bool Equals(InstalledFile other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return Id == other.Id && string.Equals(Filename, other.Filename) && Folder == other.Folder &&
			       FileLength == other.FileLength;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj is InstalledFile && Equals((InstalledFile) obj);
		}

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