using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SharpRemote.Attributes;

namespace SharpRemote.Test.Types.Classes
{
	[DataContract]
	public class ClassWithSerializationCallbacks
	{
		public List<string> Callbacks;

		public ClassWithSerializationCallbacks()
		{
			Callbacks = new List<string>();
		}

		private void CallbackInvoked([CallerMemberName] string methodName = null)
		{
			Callbacks.Add(methodName);
		}

		[BeforeDeserialize]
		public void BeforeDeserialization()
		{
			CallbackInvoked();
		}

		[AfterDeserialize]
		public void AfterDeserialization()
		{
			CallbackInvoked();
		}

		[BeforeSerialize]
		public void BeforeSerialization()
		{
			CallbackInvoked();
		}

		[AfterSerialize]
		public void AfterSerialization()
		{
			CallbackInvoked();
		}
	}
}