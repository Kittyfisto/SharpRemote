using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Structs
{
	[DataContract]
	public struct FieldIEnumerable
	{
		[DataMember]
		public IEnumerable<string> Values;
	}
}
