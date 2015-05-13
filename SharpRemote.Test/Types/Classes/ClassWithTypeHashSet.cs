using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public sealed class ClassWithTypeHashSet
	{
		[DataMember]
		public HashSet<Type> Values { get; set; }
	}
}