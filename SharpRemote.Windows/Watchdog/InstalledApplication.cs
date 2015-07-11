using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharpRemote.Watchdog
{
	/// <summary>
	/// Represents an installed application and all of the files it consists of.
	/// </summary>
	[DataContract]
	public sealed class InstalledApplication
	{
		/// <summary>
		/// Shortcut to <see cref="ApplicationDescriptor.Name"/>.
		/// </summary>
		public string Name { get { return Descriptor.Name; } }

		/// <summary>
		/// Describes this application - name, version, etc...
		/// </summary>
		[DataMember] public ApplicationDescriptor Descriptor;

		/// <summary>
		/// The list of files this installed application consists of.
		/// </summary>
		[DataMember] public List<InstalledFile> Files;

		/// <summary>
		///     The point in time when this app was installed.
		/// </summary>
		[DataMember] public DateTime InstallationTime;

		/// <summary>
		/// Ctor for deserialization.
		/// </summary>
		public InstalledApplication()
		{}

		/// <summary>
		/// Creates a new (empty) installed application from the given description.
		/// </summary>
		/// <param name="description"></param>
		/// <exception cref="ArgumentNullException">When <paramref name="description"/> is null</exception>
		/// <exception cref="ArgumentNullException">When description.Name is null</exception>
		public InstalledApplication(ApplicationDescriptor description)
		{
			if (description == null) throw new ArgumentNullException("description");
// ReSharper disable NotResolvedInText
			if (description.Name == null) throw new ArgumentNullException("description.Name");
// ReSharper restore NotResolvedInText

			Descriptor = description;
			Files = new List<InstalledFile>();
		}
	}
}