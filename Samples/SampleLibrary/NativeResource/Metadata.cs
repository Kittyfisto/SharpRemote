using System.Runtime.Serialization;

namespace SampleLibrary.NativeResource
{
	/// <summary>
	/// Example of a custom class that can be seamlessly marshalled across processes.
	/// </summary>
	[DataContract]
	public class Metadata
	{
		[DataMember]
		public string FileName { get; set; }

		[DataMember]
		public long FileSize { get; set; }
	}
}