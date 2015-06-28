using System.Runtime.Serialization;
using System.ServiceModel;
using System.Xml.Serialization;

namespace SharpRemote.Watchdog
{
	/// <summary>
	/// Describes a running instance of an application, e.g. the desire to have a running executable from
	/// the given installation.
	/// </summary>
	[DataContract]
	[XmlSerializerFormat]
	public sealed class ApplicationInstanceDescription
	{
		/// <summary>
		/// Name of this instance, must be unique amongst all others.
		/// </summary>
		[DataMember]
		[XmlAttribute]
		public string Name;

		/// <summary>
		/// The id of the application in question.
		/// </summary>
		[DataMember]
		[XmlAttribute]
		public string ApplicationName;

		/// <summary>
		/// The executable to start.
		/// </summary>
		[DataMember]
		public InstalledFile Executable;
	}
}