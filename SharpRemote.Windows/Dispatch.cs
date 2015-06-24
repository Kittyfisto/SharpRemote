using System.Runtime.Serialization;

namespace SharpRemote
{
	[DataContract]
	public enum Dispatch
	{
		[EnumMember] OncePerMethod,
		[EnumMember] OncePerObject,
		[EnumMember] OncePerType
	}
}