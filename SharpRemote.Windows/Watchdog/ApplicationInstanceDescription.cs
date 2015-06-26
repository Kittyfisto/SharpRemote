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
		/// Identifies this instance - will be set by the watchdog upon registration.
		/// </summary>
		[DataMember]
		[XmlAttribute]
		public long? Id;

		/// <summary>
		/// The name of this instance - helps debugging.
		/// </summary>
		[DataMember]
		[XmlAttribute]
		public string Name;

		/// <summary>
		/// The id of the application in question.
		/// </summary>
		[DataMember]
		[XmlAttribute]
		public long AppId;

		/// <summary>
		/// The executable to start.
		/// </summary>
		[DataMember]
		public InstalledFile Executable;
	}
}