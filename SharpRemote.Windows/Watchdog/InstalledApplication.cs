using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharpRemote.Watchdog
{
	[DataContract]
	public sealed class InstalledApplication
	{
		[DataMember] public long Id;

		[DataMember] public ApplicationDescriptor Descriptor;

		[DataMember] public List<InstalledFile> Files;

		/// <summary>
		///     The point in time when this app was installed.
		/// </summary>
		[DataMember] public DateTime InstallationTime;

		public InstalledApplication()
		{}

		public InstalledApplication(long id, ApplicationDescriptor description)
		{
			Id = id;
			Descriptor = description;
			Files = new List<InstalledFile>();
		}
	}
}