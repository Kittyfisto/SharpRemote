using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharpRemote.Watchdog
{
	[DataContract]
	public sealed class InstalledApplication
	{
		public string Name { get { return Descriptor.Name; } }

		[DataMember] public ApplicationDescriptor Descriptor;

		[DataMember] public List<InstalledFile> Files;

		/// <summary>
		///     The point in time when this app was installed.
		/// </summary>
		[DataMember] public DateTime InstallationTime;

		public InstalledApplication()
		{}

		public InstalledApplication(ApplicationDescriptor description)
		{
			if (description == null) throw new ArgumentNullException("description");
			if (description.Name == null) throw new ArgumentNullException("description.Name");

			Descriptor = description;
			Files = new List<InstalledFile>();
		}
	}
}