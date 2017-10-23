using System;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public class ClassWithAfterDeserializeCallback
	{
		public Type Type { get; set; }

		[DataMember]
		public string SerializedType { get; set; }

		[AfterDeserialize]
		public void AfterDeserialize()
		{
			// Please don't do stuff this way on your production code.
			// (Change the accessors of either of the two properties above).
			// It's just a stupid example of how to use the callback and test
			// that the callback and property accesses are done in the correct order.
			Type = Type.GetType(SerializedType);
		}
	}
}