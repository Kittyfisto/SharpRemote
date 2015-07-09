using System.Runtime.Serialization;

namespace SharpRemote
{
	[DataContract]
	public enum Dispatch
	{
		[EnumMember] DoNotSerialize = 0,
		[EnumMember] SerializePerMethod = 1,
		[EnumMember] SerializePerObject = 2,
		[EnumMember] SerializePerType = 3,
	}
}