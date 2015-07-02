using System.Runtime.Serialization;

namespace SampleBrowser.Scenarios.LongTermUsage
{
	[DataContract]
	public sealed class DataPacket
	{
		[DataMember] public int SequenceNumber;

		[DataMember]
		public byte[] Data;
	}
}