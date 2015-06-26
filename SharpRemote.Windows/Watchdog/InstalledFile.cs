using System;
using System.Runtime.Serialization;

namespace SharpRemote.Watchdog
{
	[DataContract]
	public struct InstalledFile
	{
		[DataMember]
		public long Id;

		[DataMember]
		public string Filename;

		[DataMember]
		public Environment.SpecialFolder Folder;

		[DataMember]
		public long FileLength;
	}
}