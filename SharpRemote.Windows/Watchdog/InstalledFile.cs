using System;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml.Serialization;

namespace SharpRemote.Watchdog
{
	[DataContract]
	[XmlSerializerFormat]
	public sealed class InstalledFile
	{
		[DataMember]
		[XmlAttribute]
		public long Id;

		[DataMember]
		[XmlAttribute]
		public string Filename;

		[DataMember]
		[XmlAttribute]
		public Environment.SpecialFolder Folder;

		[DataMember]
		[XmlAttribute]
		public long FileLength;
	}
}